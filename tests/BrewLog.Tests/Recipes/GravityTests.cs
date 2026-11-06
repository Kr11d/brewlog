using BrewLog.Domain.Recipes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Recipes;

public sealed class GravityTests
{
    private const double GravityTolerance = 0.001;

    [Fact]
    public void Calculate_FiveKilogramsPilsner_GivesReferenceOg()
    {
        var stats = RecipeCalculator.Calculate(RecipeOf(23, Pilsner()));

        Assert.Equal(48.3, stats.ExtractPoints, 0.05);
        Assert.Equal(1.0483, stats.Og, GravityTolerance);
    }

    [Theory]
    [InlineData(IngredientKind.Adjunct)]
    [InlineData(IngredientKind.Malt)]
    public void Calculate_NotMashedExtract_IgnoresEfficiency(IngredientKind kind)
    {
        var line = IngredientFactory.FromData(new IngredientData(kind)
        {
            Name = "Екстракт",
            Amount = 0.5,
            YieldPercent = 100,
            IsMashed = false,
        });

        var stats = RecipeCalculator.Calculate(RecipeOf(23, line));

        Assert.Equal(8.38, stats.ExtractPointsOf(line), 0.01);
    }

    [Fact]
    public void Calculate_Sucrose_AddsNoColor()
    {
        var stats = RecipeCalculator.Calculate(RecipeOf(23, Sucrose()));

        Assert.Equal(0, stats.Mcu);
        Assert.Equal(0, stats.Srm);
    }

    [Fact]
    public void Calculate_OgFiftyAttenuation75_GivesReferenceFgAndAbv()
    {
        var stats = RecipeCalculator.Calculate(OgFiftyRecipe(Us05(attenuationPercent: 75)));

        Assert.Equal(1.050, stats.Og, GravityTolerance);
        Assert.Equal(1.0125, stats.Fg, GravityTolerance);
        Assert.Equal(4.92, stats.Abv, 0.01);
    }

    [Theory]
    [InlineData(new double[] { }, 1.0125)]
    [InlineData(new[] { 60.0 }, 1.020)]
    [InlineData(new[] { 60.0, 80.0 }, 1.010)]
    [InlineData(new[] { 80.0, 60.0 }, 1.010)]
    public void Calculate_Yeasts_FgUsesMaxAttenuationOrDefault(double[] attenuations, double expectedFg)
    {
        var yeasts = attenuations.Select(attenuation => (Ingredient)Us05(attenuation)).ToArray();

        var stats = RecipeCalculator.Calculate(OgFiftyRecipe(yeasts));

        Assert.Equal(expectedFg, stats.Fg, GravityTolerance);
    }
}
