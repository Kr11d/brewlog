using BrewLog.Domain.Recipes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Recipes;

public sealed class ColorTests
{
    private const double ColorTolerance = 0.05;

    [Fact]
    public void Calculate_ReferenceGrist_GivesReferenceColor()
    {
        var crystal = new Malt { Name = "Crystal 60", Amount = 0.3, YieldPercent = 75, ColorLovibond = 60 };

        var stats = RecipeCalculator.Calculate(RecipeOf(23, Pilsner(), crystal));

        Assert.Equal(9.80, stats.Mcu, ColorTolerance);
        Assert.Equal(7.14, stats.Srm, ColorTolerance);
        Assert.Equal(14.06, stats.Ebc, ColorTolerance);
    }

    [Theory]
    [InlineData(23.0, false)]
    [InlineData(0.0, false)]
    [InlineData(0.0, true)]
    public void Calculate_EmptyRecipeOrZeroVolume_StaysFinite(double volumeL, bool withIngredients)
    {
        Ingredient[] ingredients = withIngredients ? [Pilsner(), Sucrose(), BoilHop(), Us05()] : [];

        var stats = RecipeCalculator.Calculate(RecipeOf(volumeL, ingredients));

        double[] results = [stats.Og, stats.Fg, stats.Abv, stats.Ibu, stats.Srm, stats.Ebc];
        Assert.All(results, result => Assert.True(double.IsFinite(result)));
    }

    [Theory]
    [InlineData(0.0, 255, 255, 255)]
    [InlineData(10.0, 198, 71, 7)]
    public void ToRgb_KnownSrm_GivesExpectedColor(double srm, int red, int green, int blue)
    {
        var (r, g, b) = SrmColor.ToRgb(srm);

        Assert.InRange(r, red - 1, red + 1);
        Assert.InRange(g, green - 1, green + 1);
        Assert.InRange(b, blue - 1, blue + 1);
    }
}
