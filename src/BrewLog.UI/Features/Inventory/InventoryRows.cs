using BrewLog.Domain.Inventory;
using BrewLog.Domain.Recipes;
using StockInventory = BrewLog.Domain.Inventory.Inventory;

namespace BrewLog.UI.Features.Inventory;

/// <summary>Збирає рядки зі складів усіх типів. Назву типу задає виклик generic-методу, тож перевірок типу інгредієнта немає.</summary>
public static class InventoryRows
{
    public static IReadOnlyList<InventoryRowViewModel> Build(StockInventory inventory, DateTime now, IReadOnlyDictionary<int, string> suppliers) =>
    [
        .. Rows(inventory.Malts, "Солод", now, suppliers),
        .. Rows(inventory.Hops, "Хміль", now, suppliers),
        .. Rows(inventory.Yeasts, "Дріжджі", now, suppliers),
        .. Rows(inventory.Adjuncts, "Додатки", now, suppliers),
    ];

    private static IEnumerable<InventoryRowViewModel> Rows<T>(
        Stock<T> stock,
        string kindTitle,
        DateTime now,
        IReadOnlyDictionary<int, string> suppliers) where T : Ingredient =>
        stock.Entries.Select(entry => InventoryRowViewModel.From(entry, kindTitle, now, suppliers));
}
