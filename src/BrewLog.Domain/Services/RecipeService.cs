using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Users;

namespace BrewLog.Domain.Services;

/// <summary>Робота з рецептами: перегляд, збереження, дублювання, видалення, обмін BeerXML і оцінка собівартості з перевіркою дозволів.</summary>
public sealed class RecipeService
{
    private readonly IRecipeRepository _recipes;
    private readonly IIngredientCatalogRepository _catalog;
    private readonly IBeerXmlSerializer _beerXml;
    private readonly ICurrentSession _session;
    private readonly IClock _clock;
    private readonly InventoryService _inventory;

    public RecipeService(
        IRecipeRepository recipes,
        IIngredientCatalogRepository catalog,
        IBeerXmlSerializer beerXml,
        ICurrentSession session,
        IClock clock,
        InventoryService inventory)
    {
        _recipes = recipes;
        _catalog = catalog;
        _beerXml = beerXml;
        _session = session;
        _clock = clock;
        _inventory = inventory;
    }

    public IReadOnlyList<Recipe> GetAll()
    {
        _session.Require(Permission.ViewRecipes);
        return _recipes.GetAll();
    }

    public Recipe? Get(int id)
    {
        _session.Require(Permission.ViewRecipes);
        return _recipes.GetById(id);
    }

    /// <summary>Рахує показники рецепта; дозволу не потребує, бо не читає й не змінює збережених даних.</summary>
    public RecipeStats Calculate(Recipe r) => RecipeCalculator.Calculate(r);

    /// <summary>Перевіряє й зберігає рецепт разом із рядками; повертає його ідентифікатор.</summary>
    public int Save(Recipe r)
    {
        _session.Require(Permission.EditRecipes);
        DomainException.ThrowIfAny(r.Validate());

        r.UpdatedAt = _clock.Now;
        if (r.Id == 0)
        {
            r.CreatedAt = r.UpdatedAt;
            r.AuthorId = _session.User?.Id;
            return _recipes.Insert(r);
        }

        _recipes.Update(r);
        return r.Id;
    }

    /// <summary>Видаляє рецепт разом із рядками. Рецепт із партіями лишається: партії на нього посилаються.</summary>
    public void Delete(int id)
    {
        _session.Require(Permission.EditRecipes);
        if (_recipes.IsUsedInBatches(id))
        {
            throw new DomainException("Рецепт використовується в партіях");
        }

        _recipes.Delete(id);
    }

    /// <summary>Створює й зберігає копію рецепта з назвою «&lt;назва&gt; (копія)» і незалежними рядками.</summary>
    public Recipe Duplicate(int id)
    {
        _session.Require(Permission.EditRecipes);

        var source = _recipes.GetById(id) ?? throw new DomainException("Рецепт не знайдено.");
        var copy = source.CopyAs($"{source.Name} (копія)");
        Save(copy);
        return copy;
    }

    /// <summary>
    /// Імпортує рецепти з файлу BeerXML. Рядки прив'язуються до позицій довідника за типом і назвою без регістру,
    /// а відсутні позиції створюються з характеристиками з файлу (мінімум і ціна 0).
    /// Спершу перевіряється весь файл: помилка в одному рецепті не лишає в БД половину імпорту.
    /// </summary>
    public RecipeImportResult Import(string path)
    {
        _session.Require(Permission.EditRecipes);

        var recipes = ReadRecipes(path);
        DomainException.ThrowIfAny(recipes.SelectMany(recipe => recipe.Validate()).ToList());

        var newCatalogItems = 0;
        foreach (var line in recipes.SelectMany(recipe => recipe.Ingredients))
        {
            if (BindToCatalog(line))
            {
                newCatalogItems++;
            }
        }

        return new RecipeImportResult(recipes.Select(Save).ToList(), newCatalogItems);
    }

    /// <summary>Експортує рецепти у файл BeerXML. Файл пишеться лише після успішної серіалізації, тож збій не зіпсує наявний.</summary>
    public void Export(IEnumerable<int> recipeIds, string path)
    {
        _session.Require(Permission.EditRecipes);

        var recipes = recipeIds
            .Select(id => _recipes.GetById(id) ?? throw new DomainException("Рецепт не знайдено."))
            .ToList();

        using var buffer = new MemoryStream();
        _beerXml.Export(recipes, buffer);

        try
        {
            File.WriteAllBytes(path, buffer.ToArray());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new DomainException($"Не вдалося записати файл «{Path.GetFileName(path)}». Перевірте, що він не відкритий в іншій програмі й що є дозвіл на запис.", exception);
        }
    }

    /// <summary>Собівартість сировини, грн/л: Σ кількість · ціна одиниці / обсяг; ціну одиниці дає склад (HANDOFF §6.1).</summary>
    public decimal EstimateCostPerLiter(Recipe r)
    {
        if (!(r.BatchVolumeL > 0))
        {
            return 0;
        }

        var total = r.Ingredients.Sum(line => (decimal)line.Amount * _inventory.CurrentUnitPrice(line.CatalogId));
        return total / (decimal)r.BatchVolumeL;
    }

    private IReadOnlyList<Recipe> ReadRecipes(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return _beerXml.Import(stream);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new DomainException($"Не вдалося прочитати файл «{Path.GetFileName(path)}». Перевірте, що він існує й не відкритий в іншій програмі.", exception);
        }
    }

    /// <summary>Прив'язує рядок до позиції довідника, за потреби створюючи її; true — позицію створено.</summary>
    private bool BindToCatalog(Ingredient line)
    {
        var item = _catalog.FindByName(line.Kind, line.Name);
        var isNew = item is null;
        if (item is null)
        {
            item = IngredientFactory.FromData(line.ToData() with { Id = 0, CatalogId = 0, Amount = 0 });
            _catalog.Insert(item);
        }

        line.CatalogId = item.Id;
        line.Name = item.Name;
        return isNew;
    }
}
