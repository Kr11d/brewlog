using BrewLog.Data.Repositories;
using BrewLog.Domain.Ibu;
using BrewLog.Domain.Recipes;
using Dapper;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Data;

public sealed class RecipeRepositoryTests : IDisposable
{
    private readonly TempDatabase _database = new();
    private readonly RecipeRepository _recipes;
    private readonly IngredientCatalogRepository _catalog;

    public RecipeRepositoryTests()
    {
        _recipes = new RecipeRepository(_database.Connections);
        _catalog = new IngredientCatalogRepository(_database.Connections);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public void InsertThenGetById_AllIngredientKinds_RestoresFieldsAndOrder()
    {
        var createdAt = new DateTime(2026, 3, 15, 10, 30, 45);
        var hop = InCatalog(BoilHop());
        var dryHop = InCatalog(DryHop());
        var malt = InCatalog(Pilsner());
        var sugar = InCatalog(Sucrose());
        var yeast = InCatalog(Us05());
        var recipe = RecipeOf(23, hop, malt, yeast, sugar, dryHop);
        recipe.Name = "Тестовий ель";
        recipe.EfficiencyPercent = 75;
        recipe.BoilTimeMin = 90;
        recipe.IbuFormulaCode = new RagerFormula().Code;
        recipe.Notes = "Нотатка";
        recipe.CreatedAt = createdAt;
        recipe.UpdatedAt = createdAt;

        var id = _recipes.Insert(recipe);
        var loaded = _recipes.GetById(id);

        Assert.NotNull(loaded);
        Assert.Equal("Тестовий ель", loaded.Name);
        Assert.Equal(23, loaded.BatchVolumeL, 0.0001);
        Assert.Equal(75, loaded.EfficiencyPercent, 0.0001);
        Assert.Equal(90, loaded.BoilTimeMin);
        Assert.Equal("Rager", loaded.IbuFormulaCode);
        Assert.Equal("Нотатка", loaded.Notes);
        Assert.Equal(createdAt, loaded.CreatedAt);

        Assert.Equal(
            [typeof(Hop), typeof(Malt), typeof(Yeast), typeof(Adjunct), typeof(Hop)],
            loaded.Ingredients.Select(line => line.GetType()));
        Assert.Equal(
            recipe.Ingredients.Select(WithoutLineId),
            loaded.Ingredients.Select(WithoutLineId));
    }

    [Fact]
    public void Update_ChangedLines_RewritesThem()
    {
        var malt = InCatalog(Pilsner());
        var hop = InCatalog(BoilHop());
        var recipe = RecipeOf(23, malt, hop);
        var id = _recipes.Insert(recipe);

        recipe.RemoveIngredient(hop);
        malt.Amount = 7;
        recipe.Name = "Новий";
        _recipes.Update(recipe);
        var loaded = _recipes.GetById(id);

        Assert.NotNull(loaded);
        Assert.Equal("Новий", loaded.Name);
        var line = Assert.Single(loaded.Ingredients);
        Assert.Equal(7, line.Amount, 0.0001);
    }

    [Fact]
    public void Delete_Recipe_RemovesItsLines()
    {
        var id = _recipes.Insert(RecipeOf(23, InCatalog(Pilsner())));

        _recipes.Delete(id);

        using var connection = _database.Connections.Open();
        Assert.Null(_recipes.GetById(id));
        Assert.Equal(0, connection.ExecuteScalar<int>("SELECT COUNT(*) FROM recipe_ingredients"));
    }

    [Fact]
    public void IsUsedInBatches_RecipeWithBatch_ReturnsTrue()
    {
        var used = _recipes.Insert(RecipeOf(23));
        var unused = _recipes.Insert(RecipeOf(23));
        using var connection = _database.Connections.Open();
        connection.Execute(
            """
            INSERT INTO batches (number, recipe_id, recipe_name, volume_l, state, planned_date, planned_og, planned_fg, planned_abv, planned_srm)
            VALUES ('BL-2026-001', @used, 'Рецепт', 50, 'Planned', '2026-03-15', 1.05, 1.01, 5, 7)
            """,
            new { used });

        Assert.True(_recipes.IsUsedInBatches(used));
        Assert.False(_recipes.IsUsedInBatches(unused));
    }

    private static IngredientData WithoutLineId(Ingredient line) => line.ToData() with { Id = 0 };

    /// <summary>Рядок рецепта посилається на довідник, тому спершу кладемо туди його позицію.</summary>
    private T InCatalog<T>(T line) where T : Ingredient
    {
        _catalog.Insert(line);
        return line;
    }
}
