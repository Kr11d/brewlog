using BrewLog.Domain.Inventory;
using BrewLog.Domain.Recipes;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.Inventory;

/// <summary>Позиція складу для таблиці. Створюється generic-методом, тож один тип рядка обслуговує склади всіх типів інгредієнтів.</summary>
public sealed class InventoryRowViewModel
{
    private const int ExpiringDays = 30;

    private InventoryRowViewModel(
        Ingredient item,
        string kindTitle,
        double available,
        double minStock,
        DateTime? nearestExpiry,
        decimal? averagePrice,
        bool isBelowMinimum,
        IReadOnlyList<StockLot> lots,
        DateTime now,
        IReadOnlyDictionary<int, string> suppliers)
    {
        CatalogId = item.CatalogId;
        Name = item.Name;
        Kind = item.Kind;
        KindTitle = kindTitle;
        Unit = item.Unit;
        Available = available;
        AvailableText = DisplayFormat.Amount(available, item.Unit);
        MinStock = minStock;
        MinStockText = DisplayFormat.Amount(minStock, item.Unit);
        NearestExpiry = nearestExpiry;
        NearestExpiryText = nearestExpiry is { } date ? DisplayFormat.Date(date) : "—";
        AveragePrice = averagePrice;
        AveragePriceText = averagePrice is { } price ? DisplayFormat.MoneyValue(price) : "—";
        IsBelowMinimum = isBelowMinimum;
        IsExpired = lots.Any(lot => lot.IsExpired(now));
        IsExpiring = lots.Any(lot => lot.ExpiresWithin(now, ExpiringDays));
        Value = lots.Sum(lot => (decimal)lot.QuantityLeft * lot.UnitPrice);

        (StatusText, StatusChip) = (IsExpired, IsBelowMinimum, IsExpiring) switch
        {
            (true, _, _) => ("Прострочено", "Danger"),
            (_, true, _) => ("Нижче мінімуму", "Danger"),
            (_, _, true) => ("Спливає", "Warning"),
            _ => ("Норма", "Success"),
        };

        Lots = lots
            .OrderBy(lot => lot.ExpiresAt ?? DateTime.MaxValue)
            .Select(lot => new StockLotRowViewModel(
                $"Прихід {DisplayFormat.Date(lot.ReceivedAt)}",
                lot.SupplierId is { } supplierId ? suppliers.GetValueOrDefault(supplierId, "Без постачальника") : "Без постачальника",
                DisplayFormat.Amount(lot.QuantityLeft, item.Unit),
                DisplayFormat.Money(lot.UnitPrice),
                lot.ExpiresAt is { } expiresAt ? $"до {DisplayFormat.Date(expiresAt)}" : "без строку",
                lot.LotCode ?? "—"))
            .ToList();
    }

    public int CatalogId { get; }

    public string Name { get; }

    public IngredientKind Kind { get; }

    public string KindTitle { get; }

    public string Unit { get; }

    public double Available { get; }

    public string AvailableText { get; }

    public double MinStock { get; }

    public string MinStockText { get; }

    public DateTime? NearestExpiry { get; }

    public string NearestExpiryText { get; }

    public decimal? AveragePrice { get; }

    public string AveragePriceText { get; }

    public bool IsBelowMinimum { get; }

    public bool IsExpired { get; }

    public bool IsExpiring { get; }

    /// <summary>Вартість усіх залишків позиції, грн.</summary>
    public decimal Value { get; }

    public string StatusText { get; }

    /// <summary>Суфікс стилю чипа: Chip.Danger, Chip.Warning, Chip.Success.</summary>
    public string StatusChip { get; }

    public IReadOnlyList<StockLotRowViewModel> Lots { get; }

    public bool HasNoLots => Lots.Count == 0;

    public static InventoryRowViewModel From<T>(
        StockEntry<T> entry,
        string kindTitle,
        DateTime now,
        IReadOnlyDictionary<int, string> suppliers) where T : Ingredient =>
        new(entry.Item, kindTitle, entry.Available(now), entry.MinStock, entry.NearestExpiry, entry.AverageUnitPrice, entry.IsBelowMinimum(now), entry.Lots, now, suppliers);
}
