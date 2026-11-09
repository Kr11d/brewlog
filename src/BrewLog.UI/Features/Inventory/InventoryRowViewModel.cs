using BrewLog.Domain.Inventory;
using BrewLog.Domain.Recipes;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.Inventory;

/// <summary>Позиція складу для таблиці: готові рядки для показу. Вигляд статусу тема вибирає за суфіксом стилю чипа.</summary>
public sealed class InventoryRowViewModel
{
    public InventoryRowViewModel(StockPosition position, IReadOnlyDictionary<int, string> suppliers)
    {
        var item = position.Item;
        CatalogId = item.CatalogId;
        Name = item.Name;
        Kind = item.Kind;
        KindTitle = item.KindDisplayName;
        Unit = item.Unit;
        Available = position.Available;
        AvailableText = DisplayFormat.Amount(position.Available, item.Unit);
        MinStock = position.MinStock;
        MinStockText = DisplayFormat.Amount(position.MinStock, item.Unit);
        NearestExpiry = position.NearestExpiry;
        NearestExpiryText = position.NearestExpiry is { } date ? DisplayFormat.Date(date) : "—";
        AveragePrice = position.AverageUnitPrice;
        AveragePriceText = position.AverageUnitPrice is { } price ? DisplayFormat.MoneyValue(price) : "—";
        IsBelowMinimum = position.IsBelowMinimum;
        IsExpired = position.IsExpired;
        IsExpiring = position.IsExpiring;
        Value = position.Value;
        StatusText = position.StatusText;
        StatusChip = position.Status switch
        {
            StockStatus.Normal => "Success",
            StockStatus.Expiring => "Warning",
            _ => "Danger",
        };

        Lots = position.Lots
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
}
