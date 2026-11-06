using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Recipes;

namespace BrewLog.Tests.Fakes;

/// <summary>Заглушка сховища рецептів: тест сам задає, які рецепти вже мають партії.</summary>
public sealed class FakeRecipeRepository : InMemoryRepository<Recipe>, IRecipeRepository
{
    public HashSet<int> UsedInBatches { get; } = [];

    public bool IsUsedInBatches(int id) => UsedInBatches.Contains(id);
}
