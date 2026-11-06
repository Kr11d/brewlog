using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Users;

namespace BrewLog.Domain.Services;

/// <summary>Робота з рецептами: перегляд, збереження, дублювання, видалення й оцінка собівартості з перевіркою дозволів.</summary>
public sealed class RecipeService
{
    private readonly IRecipeRepository _recipes;
    private readonly IIngredientCatalogRepository _catalog;
    private readonly ICurrentSession _session;
    private readonly IClock _clock;

    public RecipeService(
        IRecipeRepository recipes,
        IIngredientCatalogRepository catalog,
        ICurrentSession session,
        IClock clock)
    {
        _recipes = recipes;
        _catalog = catalog;
        _session = session;
        _clock = clock;
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
    /// Собівартість сировини, грн/л: Σ кількість · ціна одиниці / обсяг.
    /// Поки ціна — остання з довідника; перехід на ціни складу робить промт A1.
    /// </summary>
    public decimal EstimateCostPerLiter(Recipe r)
    {
        if (!(r.BatchVolumeL > 0))
        {
            return 0;
        }

        var prices = _catalog.GetLastPrices();
        var total = r.Ingredients.Sum(line => (decimal)line.Amount * prices.GetValueOrDefault(line.CatalogId));
        return total / (decimal)r.BatchVolumeL;
    }
}
