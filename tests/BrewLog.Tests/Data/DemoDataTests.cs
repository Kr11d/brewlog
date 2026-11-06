using BrewLog.Data.Repositories;
using BrewLog.Data.Seed;
using BrewLog.Domain.Recipes;
using BrewLog.Tests.Fakes;

namespace BrewLog.Tests.Data;

public sealed class DemoDataTests : IDisposable
{
    private readonly TempDatabase _database = new(DemoSeeders.All(new FakePasswordHasher()));

    public void Dispose() => _database.Dispose();

    [Theory]
    [InlineData("Czech Pils", "3B")]
    [InlineData("American IPA", "21A")]
    [InlineData("Irish Stout", "15B")]
    [InlineData("Weissbier", "10A")]
    [InlineData("American Pale Ale", "18B")]
    public void DemoRecipe_AnyOfFive_IsValidAndWithinItsStyle(string recipeName, string styleCode)
    {
        var recipe = new RecipeRepository(_database.Connections).GetAll().Single(item => item.Name == recipeName);
        var style = new StyleRepository(_database.Connections).GetById(recipe.StyleId!.Value);

        Assert.NotNull(style);
        Assert.Equal(styleCode, style.Code);
        Assert.Empty(recipe.Validate());
        Assert.All(
            StyleCheck.Check(style, RecipeCalculator.Calculate(recipe)),
            check => Assert.True(check.Status == StyleCheckStatus.Within, $"{check.Parameter} = {check.Value} поза {check.Range}"));
    }

    [Fact]
    public void DemoCatalog_AllItems_AreValidAndComplete()
    {
        var catalog = new IngredientCatalogRepository(_database.Connections);
        var styles = new StyleRepository(_database.Connections).GetAll();

        Assert.Equal(15, styles.Count);
        Assert.All(styles, style => Assert.Empty(style.Validate()));
        Assert.Equal(24, catalog.GetAll().Count);
        Assert.All(catalog.GetAll(), item => Assert.Empty(item.ValidateCatalogItem()));
        Assert.All(catalog.GetLastPrices().Values, price => Assert.True(price > 0));
    }
}
