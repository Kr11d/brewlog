namespace BrewLog.UI.Features.Inventory;

/// <summary>Вибір постачальника замовлення; Id = null означає «не вказано».</summary>
public sealed record SupplierOption(int? Id, string Title);
