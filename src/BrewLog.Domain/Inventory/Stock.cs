using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Inventory;

/// <summary>Склад одного типу інгредієнта. Узагальнений клас — статичний поліморфізм (ЛР 1): той самий код обслуговує солод, хміль, дріжджі й додатки.</summary>
public sealed class Stock<T> : IStock where T : Ingredient
{
    private readonly List<StockEntry<T>> _entries;

    public Stock(IEnumerable<StockEntry<T>> entries)
    {
        _entries = entries.ToList();
    }

    public IReadOnlyList<StockEntry<T>> Entries => _entries;

    public StockEntry<T>? Find(int catalogId) => _entries.FirstOrDefault(entry => entry.Item.CatalogId == catalogId);

    public IReadOnlyList<StockEntry<T>> BelowMinimum(DateTime now) => _entries.Where(entry => entry.IsBelowMinimum(now)).ToList();

    /// <summary>Позиції, у яких є партія зі строком, що спливає впродовж указаної кількості днів.</summary>
    public IReadOnlyList<StockEntry<T>> Expiring(DateTime now, int days) =>
        _entries.Where(entry => entry.Lots.Any(lot => lot.ExpiresWithin(now, days))).ToList();

    public bool Contains(int catalogId) => Find(catalogId) is not null;

    public double AvailableOf(int catalogId, DateTime now) => Find(catalogId)?.Available(now) ?? 0;

    /// <summary>
    /// План списання FIFO: спершу партія з найближчим строком, за рівного строку — раніше отримана, партії без строку останні.
    /// Прострочені партії пропускаються. Нічого не змінює; якщо сировини бракує, кидає InsufficientStockException.
    /// </summary>
    public IReadOnlyList<WriteOffPart> PlanWriteOff(int catalogId, double quantity, DateTime now)
    {
        var entry = Find(catalogId)
            ?? throw new InsufficientStockException([new Shortage($"Позиція №{catalogId}", "од.", quantity, 0)]);

        var available = entry.Available(now);
        if (available + StockLot.QuantityTolerance < quantity)
        {
            throw new InsufficientStockException([new Shortage(entry.Item.Name, entry.Item.Unit, quantity, available)]);
        }

        var usableLots = entry.Lots
            .Where(lot => !lot.IsExpired(now))
            .OrderBy(lot => lot.ExpiresAt?.Date ?? DateTime.MaxValue)
            .ThenBy(lot => lot.ReceivedAt)
            .ThenBy(lot => lot.Id);

        var parts = new List<WriteOffPart>();
        var remaining = quantity;
        foreach (var lot in usableLots)
        {
            if (remaining <= StockLot.QuantityTolerance)
            {
                break;
            }

            var taken = Math.Min(lot.QuantityLeft, remaining);
            parts.Add(new WriteOffPart(lot.Id, catalogId, taken, lot.UnitPrice));
            remaining -= taken;
        }

        return parts;
    }
}
