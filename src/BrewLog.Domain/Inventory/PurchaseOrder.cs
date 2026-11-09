namespace BrewLog.Domain.Inventory;

/// <summary>Замовлення постачальнику; постачальника може не бути, якщо його ще не вибрано.</summary>
public sealed record PurchaseOrder(Supplier? Supplier, DateTime CreatedAt, IReadOnlyList<PurchaseOrderLine> Lines)
{
    public decimal Total => Lines.Sum(line => line.Total);
}
