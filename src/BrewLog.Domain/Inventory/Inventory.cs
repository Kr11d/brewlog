using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Inventory;

/// <summary>Увесь склад: окремий Stock для кожного типу інгредієнта й операції за рецептом.</summary>
public sealed class Inventory
{
    private readonly IReadOnlyList<IStock> _stocks;

    /// <param name="catalog">Позиції довідника.</param>
    /// <param name="lots">Партії із залишком.</param>
    /// <param name="minStocks">Мінімальні запаси за ідентифікатором позиції; відсутній запис означає 0.</param>
    public Inventory(IEnumerable<Ingredient> catalog, IEnumerable<StockLot> lots, IReadOnlyDictionary<int, double> minStocks)
    {
        var items = catalog.ToList();
        var lotsByItem = lots.ToLookup(lot => lot.CatalogId);

        Malts = Build<Malt>(items, lotsByItem, minStocks);
        Hops = Build<Hop>(items, lotsByItem, minStocks);
        Yeasts = Build<Yeast>(items, lotsByItem, minStocks);
        Adjuncts = Build<Adjunct>(items, lotsByItem, minStocks);
        _stocks = [Malts, Hops, Yeasts, Adjuncts];
    }

    public Stock<Malt> Malts { get; }

    public Stock<Hop> Hops { get; }

    public Stock<Yeast> Yeasts { get; }

    public Stock<Adjunct> Adjuncts { get; }

    /// <summary>
    /// Потреба сировини на партію за ідентифікатором позиції: кількість у рецепті × масштаб (HANDOFF §6.2).
    /// Кілька рядків однієї позиції (хміль на кипіння й сухе охмелення) додаються.
    /// </summary>
    public static IReadOnlyDictionary<int, double> Requirement(Recipe recipe, double volumeL)
    {
        var scale = recipe.ScaleFactor(volumeL);
        return recipe.Ingredients
            .GroupBy(line => line.CatalogId)
            .ToDictionary(group => group.Key, group => group.Sum(line => line.Amount) * scale);
    }

    /// <summary>Придатний залишок позиції будь-якого типу; невідома позиція має 0.</summary>
    public double Available(int catalogId, DateTime now) => StockOf(catalogId)?.AvailableOf(catalogId, now) ?? 0;

    /// <summary>Порівнює потребу рецепта з придатними залишками й називає все, чого бракує.</summary>
    public StockCheckResult Check(Recipe recipe, double volumeL, DateTime now)
    {
        var shortages = new List<Shortage>();
        foreach (var (catalogId, required) in Requirement(recipe, volumeL))
        {
            var available = Available(catalogId, now);
            if (available + StockLot.QuantityTolerance < required)
            {
                var line = recipe.Ingredients.First(item => item.CatalogId == catalogId);
                shortages.Add(new Shortage(line.Name, line.Unit, required, available));
            }
        }

        return new StockCheckResult(shortages.Count == 0, shortages);
    }

    /// <summary>План списання всієї сировини партії. Спершу перевіряє все, тож у винятку одразу перелічені всі нестачі.</summary>
    public IReadOnlyList<WriteOffPart> PlanWriteOff(Recipe recipe, double volumeL, DateTime now)
    {
        var check = Check(recipe, volumeL, now);
        if (!check.IsSufficient)
        {
            throw new InsufficientStockException(check.Shortages);
        }

        return Requirement(recipe, volumeL)
            .SelectMany(need => StockOf(need.Key)?.PlanWriteOff(need.Key, need.Value, now) ?? [])
            .ToList();
    }

    private static Stock<T> Build<T>(
        IEnumerable<Ingredient> items,
        ILookup<int, StockLot> lotsByItem,
        IReadOnlyDictionary<int, double> minStocks) where T : Ingredient =>
        new(items.OfType<T>().Select(item => new StockEntry<T>(item, minStocks.GetValueOrDefault(item.CatalogId), lotsByItem[item.CatalogId])));

    private IStock? StockOf(int catalogId) => _stocks.FirstOrDefault(stock => stock.Contains(catalogId));
}
