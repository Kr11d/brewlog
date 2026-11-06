using BrewLog.Domain.Recipes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Recipes;

public sealed class RecipeTests
{
    [Fact]
    public void AddAndRemoveIngredient_Lines_KeepOrderOfRemaining()
    {
        var malt = Pilsner();
        var hop = BoilHop();
        var yeast = Us05();
        var recipe = RecipeOf(23, malt, hop, yeast);

        recipe.RemoveIngredient(hop);

        Assert.Equal([malt, yeast], recipe.Ingredients);
    }

    [Theory]
    [InlineData(0, new[] { "Magnum", "Pilsner", "US-05" })]
    [InlineData(2, new[] { "Pilsner", "US-05", "Magnum" })]
    [InlineData(99, new[] { "Pilsner", "US-05", "Magnum" })]
    [InlineData(-1, new[] { "Magnum", "Pilsner", "US-05" })]
    public void MoveIngredient_NewIndex_ReordersWithinBounds(int newIndex, string[] expectedNames)
    {
        var hop = BoilHop();
        var recipe = RecipeOf(23, Pilsner(), hop, Us05());

        recipe.MoveIngredient(hop, newIndex);

        Assert.Equal(expectedNames, recipe.Ingredients.Select(ingredient => ingredient.Name));
    }

    [Fact]
    public void IngredientsOf_Type_ReturnsOnlyLinesOfThatType()
    {
        var firstHop = BoilHop();
        var secondHop = BoilHop();
        var recipe = RecipeOf(23, Pilsner(), firstHop, Us05(), secondHop);

        var hops = recipe.IngredientsOf<Hop>();

        Assert.Equal([firstHop, secondHop], hops);
    }

    [Fact]
    public void ScaleFactor_FiftyLitersFrom23_GivesReferenceFactor()
    {
        var recipe = RecipeOf(23);

        Assert.Equal(2.174, recipe.ScaleFactor(50), 0.001);
    }

    [Fact]
    public void Validate_ValidRecipe_ReturnsNoErrors()
    {
        var recipe = RecipeOf(23, Pilsner(), BoilHop(), Us05());

        Assert.Empty(recipe.Validate());
    }

    [Fact]
    public void Validate_ZeroVolume_ReturnsError()
    {
        var recipe = RecipeOf(0, Pilsner());

        Assert.NotEmpty(recipe.Validate());
    }

    [Fact]
    public void Validate_InvalidLine_IncludesItsError()
    {
        var malt = Pilsner();
        malt.Amount = 0;
        var recipe = RecipeOf(23, malt);

        var error = Assert.Single(recipe.Validate());

        Assert.Contains("Pilsner", error);
    }
}
