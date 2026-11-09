using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Inventory;

/// <summary>Позиція складу будь-якого типу на певний момент: те, що показує сторінка «Склад» і експорт в Excel.</summary>
public sealed class StockPosition
{
    /// <summary>За скільки днів до кінця строку партія вважається такою, що спливає.</summary>
    public const int ExpiringDays = 30;

    public StockPosition(Ingredient item, double minStock, double available, DateTime? nearestExpiry, decimal? averageUnitPrice, bool isBelowMinimum, IReadOnlyList<StockLot> lots, DateTime now)
    {
        Item = item;
        MinStock = minStock;
        Available = available;
        NearestExpiry = nearestExpiry;
        AverageUnitPrice = averageUnitPrice;
        IsBelowMinimum = isBelowMinimum;
        Lots = lots;
        IsExpired = lots.Any(lot => lot.IsExpired(now));
        IsExpiring = lots.Any(lot => lot.ExpiresWithin(now, ExpiringDays));
        Value = lots.Sum(lot => (decimal)lot.QuantityLeft * lot.UnitPrice);
    }

    public Ingredient Item { get; }

    public double MinStock { get; }

    /// <summary>Придатний залишок, без прострочених партій.</summary>
    public double Available { get; }

    public DateTime? NearestExpiry { get; }

    public decimal? AverageUnitPrice { get; }

    public bool IsBelowMinimum { get; }

    public bool IsExpired { get; }

    public bool IsExpiring { get; }

    /// <summary>Вартість усіх залишків позиції, зокрема прострочених, грн.</summary>
    public decimal Value { get; }

    public IReadOnlyList<StockLot> Lots { get; }

    /// <summary>Найважливіший стан позиції: прострочена партія — спершу, далі нестача, далі строк.</summary>
    public StockStatus Status => (IsExpired, IsBelowMinimum, IsExpiring) switch
    {
        (true, _, _) => StockStatus.Expired,
        (_, true, _) => StockStatus.BelowMinimum,
        (_, _, true) => StockStatus.Expiring,
        _ => StockStatus.Normal,
    };

    public string StatusText => Status switch
    {
        StockStatus.Expired => "Прострочено",
        StockStatus.BelowMinimum => "Нижче мінімуму",
        StockStatus.Expiring => "Спливає",
        _ => "Норма",
    };
}
