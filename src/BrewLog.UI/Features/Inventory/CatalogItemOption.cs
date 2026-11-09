namespace BrewLog.UI.Features.Inventory;

/// <summary>Позиція довідника у виборі діалогу приходу; Title містить назву й тип.</summary>
public sealed record CatalogItemOption(int CatalogId, string Title, string Unit);
