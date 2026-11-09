namespace BrewLog.Domain.Inventory;

/// <summary>Рядок замовлення постачальнику.</summary>
public sealed record PurchaseOrderLine(int CatalogId, string Name, string KindName, double Quantity, string Unit, decimal UnitPrice)
{
    public decimal Total => (decimal)Quantity * UnitPrice;
}
