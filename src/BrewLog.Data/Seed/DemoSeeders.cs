using BrewLog.Domain.Abstractions;

namespace BrewLog.Data.Seed;

/// <summary>
/// Повний список демо-сідерів для AppServices, SmokeRunner і тестів.
/// Порядок (Order): 10 UserSeeder (B) → 20 CatalogSeeder (B) → 30 RecipeSeeder (B) → 40 StockSeeder (A) → 50 BatchSeeder (A).
/// Поки порожній: сідери з'являться у своїх промтах.
/// </summary>
public static class DemoSeeders
{
    public static IReadOnlyList<ISeeder> All(IPasswordHasher passwordHasher) => [];
}
