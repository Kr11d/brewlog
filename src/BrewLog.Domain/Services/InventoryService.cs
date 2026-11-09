using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Inventory;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Users;

namespace BrewLog.Domain.Services;

/// <summary>Склад сировини: залишки, прихід, ціни, перевірка й FIFO-списання під партію.</summary>
public sealed class InventoryService
{
    private readonly IStockRepository _stock;
    private readonly IIngredientCatalogRepository _catalog;
    private readonly IRecipeRepository _recipes;
    private readonly ICurrentSession _session;
    private readonly IClock _clock;

    public InventoryService(
        IStockRepository stock,
        IIngredientCatalogRepository catalog,
        IRecipeRepository recipes,
        ICurrentSession session,
        IClock clock)
    {
        _stock = stock;
        _catalog = catalog;
        _recipes = recipes;
        _session = session;
        _clock = clock;
    }

    /// <summary>Склад на момент now: партії, отримані пізніше, ще не враховуються.</summary>
    public Inventory.Inventory GetInventory(DateTime now)
    {
        _session.Require(Permission.ViewStock);
        return Load(now);
    }

    /// <summary>
    /// Чи вистачає сировини на партію. Дозволу складу не вимагає: перевірку запускає BatchService,
    /// а результат містить лише нестачі цього рецепта.
    /// </summary>
    public StockCheckResult CheckAvailability(Recipe recipe, double volumeL)
    {
        var now = _clock.Now;
        return Load(now).Check(recipe, volumeL, now);
    }

    /// <summary>
    /// Списує сировину партії за FIFO й повертає її вартість. Потрібен дозвіл керувати партіями, а не складом:
    /// варку запускає пивовар, який склад лише переглядає.
    /// </summary>
    public decimal WriteOffForBatch(int batchId, Recipe recipe, double volumeL, int? userId)
    {
        _session.Require(Permission.ManageBatches);

        var now = _clock.Now;
        var parts = Load(now).PlanWriteOff(recipe, volumeL, now);
        _stock.ApplyWriteOff(parts, batchId, userId);
        return parts.Sum(part => part.Cost);
    }

    /// <summary>Оформлює прихід сировини поточною датою; його ціна стає останньою для позиції.</summary>
    public StockLot ReceiveStock(int catalogId, double quantity, decimal unitPrice, int? supplierId, DateTime? expiresAt, string? lotCode)
    {
        _session.Require(Permission.ManageStock);
        EnsureCatalogItem(catalogId);

        var lot = new StockLot
        {
            CatalogId = catalogId,
            SupplierId = supplierId,
            QuantityReceived = quantity,
            QuantityLeft = quantity,
            UnitPrice = unitPrice,
            ReceivedAt = _clock.Now,
            ExpiresAt = expiresAt,
            LotCode = string.IsNullOrWhiteSpace(lotCode) ? null : lotCode.Trim(),
        };
        DomainException.ThrowIfAny(lot.Validate());

        _stock.ReceiveLot(lot, _session.User?.Id);
        return lot;
    }

    public void UpdateMinStock(int catalogId, double minStock)
    {
        _session.Require(Permission.ManageStock);
        EnsureCatalogItem(catalogId);
        if (minStock < 0 || !NumberLimits.IsAllowed(minStock))
        {
            throw new DomainException("Вкажіть коректний мінімальний запас.");
        }

        _stock.UpdateMinStock(catalogId, minStock);
    }

    public void UpdatePrice(int catalogId, decimal price)
    {
        _session.Require(Permission.ManageStock);
        EnsureCatalogItem(catalogId);
        if (price < 0 || !NumberLimits.IsAllowed((double)price))
        {
            throw new DomainException("Вкажіть коректну ціну за одиницю.");
        }

        _stock.UpdateLastPrice(catalogId, price);
    }

    public IReadOnlyList<Supplier> GetSuppliers()
    {
        _session.Require(Permission.ViewStock);
        return _stock.GetSuppliers();
    }

    /// <summary>Знаходить постачальника за назвою без урахування регістру або створює нового: назва в таблиці унікальна.</summary>
    public Supplier FindOrAddSupplier(string name)
    {
        _session.Require(Permission.ManageStock);

        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            throw new DomainException("Вкажіть назву постачальника.");
        }

        var existing = _stock.GetSuppliers().FirstOrDefault(item => string.Equals(item.Name, trimmed, StringComparison.CurrentCultureIgnoreCase));
        if (existing is not null)
        {
            return existing;
        }

        var supplier = new Supplier { Name = trimmed };
        _stock.InsertSupplier(supplier);
        return supplier;
    }

    /// <summary>Ціна одиниці за HANDOFF §6.1: середньозважена по залишках, а без залишків — остання з довідника.</summary>
    public decimal CurrentUnitPrice(int catalogId) =>
        StockLot.AveragePrice(_stock.GetLotsOf(catalogId)) ?? _catalog.GetLastPrices().GetValueOrDefault(catalogId);

    /// <summary>
    /// Потреба сировини для планової партії за ідентифікатором позиції. Рецепт читається напряму зі сховища:
    /// власник не має ViewRecipes, але бачить потребу на сторінці «Склад».
    /// </summary>
    public IReadOnlyDictionary<int, double> Requirement(int recipeId, double volumeL)
    {
        _session.Require(Permission.ViewStock);

        var recipe = _recipes.GetById(recipeId) ?? throw new DomainException("Рецепт не знайдено.");
        return Inventory.Inventory.Requirement(recipe, volumeL);
    }

    private Inventory.Inventory Load(DateTime now) => new(
        _catalog.GetAll(),
        _stock.GetActiveLots().Where(lot => lot.ReceivedAt <= now),
        _catalog.GetMinStocks());

    private void EnsureCatalogItem(int catalogId)
    {
        if (_catalog.GetById(catalogId) is null)
        {
            throw new DomainException("Позицію довідника не знайдено.");
        }
    }
}
