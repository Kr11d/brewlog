using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Recipes;

namespace BrewLog.Tests.Fakes;

/// <summary>Заглушка сховища стилів: тест сам задає, які стилі вже використовуються.</summary>
public sealed class FakeStyleRepository : InMemoryRepository<BeerStyle>, IStyleRepository
{
    public HashSet<int> UsedIds { get; } = [];

    public bool IsUsed(int id) => UsedIds.Contains(id);
}
