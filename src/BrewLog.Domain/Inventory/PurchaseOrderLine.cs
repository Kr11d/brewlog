using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Inventory;

/// <summary>Рядок замовлення постачальнику.</summary>
public sealed record PurchaseOrderLine(int CatalogId, string Name, IngredientKind Kind, double Quantity, string Unit, decimal UnitPrice)
{
    public decimal Total => (decimal)Quantity * UnitPrice;
}
