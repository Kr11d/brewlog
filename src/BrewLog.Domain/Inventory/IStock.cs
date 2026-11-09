namespace BrewLog.Domain.Inventory;

/// <summary>Спільний вигляд складу будь-якого типу: рядок рецепта знає лише позицію довідника, тож Inventory обходиться без перевірок типу.</summary>
public interface IStock
{
    bool Contains(int catalogId);

    double AvailableOf(int catalogId, DateTime now);

    IReadOnlyList<WriteOffPart> PlanWriteOff(int catalogId, double quantity, DateTime now);

    IEnumerable<StockPosition> Positions(DateTime now);
}
