using BrewLog.Data.Repositories;
using BrewLog.Domain.Recipes;
using Dapper;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Data;

public sealed class IngredientCatalogRepositoryTests : IDisposable
{
    private readonly TempDatabase _database = new();
    private readonly IngredientCatalogRepository _catalog;

    public IngredientCatalogRepositoryTests()
    {
        _catalog = new IngredientCatalogRepository(_database.Connections);
    }

    public void Dispose() => _database.Dispose();

    /// <summary>COLLATE NOCASE у SQLite не знає кирилиці, тому пошук має працювати інакше.</summary>
    [Theory]
    [InlineData("САХАРОЗА")]
    [InlineData("сахароза")]
    public void FindByName_CyrillicInOtherCase_FindsItem(string name)
    {
        _catalog.Insert(Sucrose());

        var found = _catalog.FindByName(IngredientKind.Adjunct, name);

        Assert.IsType<Adjunct>(found);
        Assert.Null(_catalog.FindByName(IngredientKind.Hop, name));
    }

    [Fact]
    public void IsUsed_ItemInRecipeOrStock_ReturnsTrue()
    {
        var inRecipe = _catalog.Insert(Pilsner());
        var inStock = _catalog.Insert(BoilHop());
        var unused = _catalog.Insert(Us05());
        new RecipeRepository(_database.Connections).Insert(RecipeOf(23, new Malt
        {
            CatalogId = inRecipe,
            Name = "Pilsner",
            Amount = 5,
            YieldPercent = 80,
        }));
        using var connection = _database.Connections.Open();
        connection.Execute(
            """
            INSERT INTO stock_lots (catalog_id, quantity_received, quantity_left, unit_price, received_at)
            VALUES (@inStock, 100, 100, 2, '2026-03-15')
            """,
            new { inStock });

        Assert.True(_catalog.IsUsed(inRecipe));
        Assert.True(_catalog.IsUsed(inStock));
        Assert.False(_catalog.IsUsed(unused));
    }
}
