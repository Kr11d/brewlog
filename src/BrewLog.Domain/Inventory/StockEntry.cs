using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Inventory;

/// <summary>Одна позиція складу: інгредієнт, його мінімальний запас і партії із залишком.</summary>
public sealed class StockEntry<T> where T : Ingredient
{
    public StockEntry(T item, double minStock, IEnumerable<StockLot> lots)
    {
        Item = item;
        MinStock = minStock;
        Lots = lots.Where(lot => lot.QuantityLeft > 0).ToList();
    }

    public T Item { get; }

    public double MinStock { get; }

    /// <summary>Лише партії із залишком, зокрема й прострочені: їх треба показати на складі.</summary>
    public IReadOnlyList<StockLot> Lots { get; }

    /// <summary>Найближчий строк серед партій із залишком; прострочена партія теж рахується, бо потребує уваги першою.</summary>
    public DateTime? NearestExpiry => Lots.Min(lot => lot.ExpiresAt);

    /// <summary>Середньозважена ціна за всіма залишками (HANDOFF §6.1); null, якщо залишків немає.</summary>
    public decimal? AverageUnitPrice => StockLot.AveragePrice(Lots);

    /// <summary>Кількість, яку можна використати: прострочені партії не враховуються.</summary>
    public double Available(DateTime now) => Lots.Where(lot => !lot.IsExpired(now)).Sum(lot => lot.QuantityLeft);

    public bool IsBelowMinimum(DateTime now) => Available(now) + StockLot.QuantityTolerance < MinStock;

    /// <summary>Знімок позиції без параметра типу: так склади всіх типів можна показати в одній таблиці.</summary>
    public StockPosition ToPosition(DateTime now) =>
        new(Item, MinStock, Available(now), NearestExpiry, AverageUnitPrice, IsBelowMinimum(now), Lots, now);
}
