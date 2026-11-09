using StockInventory = BrewLog.Domain.Inventory.Inventory;

namespace BrewLog.UI.Features.Inventory;

/// <summary>Збирає рядки таблиці зі складів усіх типів.</summary>
public static class InventoryRows
{
    public static IReadOnlyList<InventoryRowViewModel> Build(StockInventory inventory, DateTime now, IReadOnlyDictionary<int, string> suppliers) =>
        inventory.Positions(now).Select(position => new InventoryRowViewModel(position, suppliers)).ToList();
}
