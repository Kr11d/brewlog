using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Recipes;

namespace BrewLog.Tests.Fakes;

/// <summary>Заглушка довідника інгредієнтів: тест сам задає використання й ціни.</summary>
public sealed class FakeCatalogRepository : InMemoryRepository<Ingredient>, IIngredientCatalogRepository
{
    public HashSet<int> UsedIds { get; } = [];

    public Dictionary<int, decimal> LastPrices { get; } = [];

    public IReadOnlyList<Ingredient> GetByKind(IngredientKind kind) =>
        GetAll().Where(item => item.Kind == kind).ToList();

    public Ingredient? FindByName(IngredientKind kind, string name) =>
        GetByKind(kind).FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));

    public bool IsUsed(int id) => UsedIds.Contains(id);

    public IReadOnlyDictionary<int, decimal> GetLastPrices() => LastPrices;

    public IReadOnlyDictionary<int, double> GetMinStocks() => new Dictionary<int, double>();
}
