namespace BrewLog.Domain.Inventory;

/// <summary>Частина списання з однієї партії складу.</summary>
public sealed record WriteOffPart(int LotId, int CatalogId, double Quantity, decimal UnitPrice)
{
    public decimal Cost => (decimal)Quantity * UnitPrice;
}
