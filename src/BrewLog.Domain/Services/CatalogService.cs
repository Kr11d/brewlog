using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Users;

namespace BrewLog.Domain.Services;

/// <summary>
/// Довідники стилів та інгредієнтів. Читати їх може кожна роль: вони потрібні і для рецептів, і для складу.
/// Змінювати — лише з дозволом EditReferences.
/// </summary>
public sealed class CatalogService
{
    private readonly IStyleRepository _styles;
    private readonly IIngredientCatalogRepository _catalog;
    private readonly ICurrentSession _session;

    public CatalogService(IStyleRepository styles, IIngredientCatalogRepository catalog, ICurrentSession session)
    {
        _styles = styles;
        _catalog = catalog;
        _session = session;
    }

    public IReadOnlyList<BeerStyle> GetStyles() => _styles.GetAll();

    /// <summary>Перевіряє й зберігає стиль; повертає його ідентифікатор.</summary>
    public int SaveStyle(BeerStyle style)
    {
        _session.Require(Permission.EditReferences);
        DomainException.ThrowIfAny(style.Validate());

        var isDuplicate = _styles.GetAll().Any(other =>
            other.Id != style.Id && string.Equals(other.Code, style.Code, StringComparison.OrdinalIgnoreCase));
        if (isDuplicate)
        {
            throw new DomainException($"Стиль із кодом {style.Code} уже є.");
        }

        if (style.Id == 0)
        {
            return _styles.Insert(style);
        }

        _styles.Update(style);
        return style.Id;
    }

    public void DeleteStyle(int id)
    {
        _session.Require(Permission.EditReferences);
        if (_styles.IsUsed(id))
        {
            throw new DomainException("Стиль використовується в рецептах.");
        }

        _styles.Delete(id);
    }

    /// <summary>Позиції довідника одного типу або всі, якщо тип не вказано.</summary>
    public IReadOnlyList<Ingredient> GetCatalog(IngredientKind? kind = null) =>
        kind is null ? _catalog.GetAll() : _catalog.GetByKind(kind.Value);

    /// <summary>Остання ціна за одиницю, грн, за ідентифікатором позиції. Змінює її власник на складі, не тут.</summary>
    public IReadOnlyDictionary<int, decimal> GetLastPrices() => _catalog.GetLastPrices();

    /// <summary>Мінімальний запас за ідентифікатором позиції. Змінює його власник на складі, не тут.</summary>
    public IReadOnlyDictionary<int, double> GetMinStocks() => _catalog.GetMinStocks();

    /// <summary>Перевіряє й зберігає позицію довідника; повертає її ідентифікатор.</summary>
    public int SaveCatalogItem(Ingredient item)
    {
        _session.Require(Permission.EditReferences);
        DomainException.ThrowIfAny(item.ValidateCatalogItem());

        var sameName = _catalog.FindByName(item.Kind, item.Name);
        if (sameName is not null && sameName.Id != item.Id)
        {
            throw new DomainException($"Позиція «{item.Name}» уже є в довіднику.");
        }

        if (item.Id == 0)
        {
            return _catalog.Insert(item);
        }

        _catalog.Update(item);
        return item.Id;
    }

    public void DeleteCatalogItem(int id)
    {
        _session.Require(Permission.EditReferences);
        if (_catalog.IsUsed(id))
        {
            throw new DomainException("Інгредієнт використовується в рецептах або на складі.");
        }

        _catalog.Delete(id);
    }
}
