namespace BrewLog.UI.Features.Inventory;

/// <summary>Партія сировини в картці вибраної позиції.</summary>
public sealed record StockLotRowViewModel(string ReceivedText, string SupplierText, string LeftText, string PriceText, string ExpiryText, string CodeText);
