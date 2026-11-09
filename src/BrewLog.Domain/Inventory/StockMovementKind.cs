namespace BrewLog.Domain.Inventory;

/// <summary>Вид руху складу; назви збігаються зі значеннями колонки stock_movements.kind.</summary>
public enum StockMovementKind
{
    Receipt,
    WriteOff,
    Adjustment,
}
