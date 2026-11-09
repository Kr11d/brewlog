namespace BrewLog.Domain.Inventory;

/// <summary>Стан позиції складу.</summary>
public enum StockStatus
{
    Normal,
    Expiring,
    BelowMinimum,
    Expired,
}
