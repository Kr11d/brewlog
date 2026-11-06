using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Recipes;

public sealed class StyleCheckTests
{
    [Theory]
    [InlineData(1.060, 1.070, StyleCheckStatus.Below, 0.0)]
    [InlineData(1.040, 1.060, StyleCheckStatus.Within, 0.5)]
    [InlineData(1.030, 1.040, StyleCheckStatus.Above, 1.0)]
    public void Check_OgAgainstRange_GivesStatusAndPosition(
        double min, double max, StyleCheckStatus expectedStatus, double expectedPosition)
    {
        var stats = RecipeCalculator.Calculate(OgFiftyRecipe());
        var style = new BeerStyle { OgRange = new ValueRange(min, max) };

        var og = StyleCheck.Check(style, stats).Single(check => check.Parameter == "OG");

        Assert.Equal(expectedStatus, og.Status);
        Assert.Equal(expectedPosition, og.Position, 0.01);
        Assert.Equal(1.050, og.Value, 0.001);
    }

    /// <summary>Кожен показник має йти зі своїм значенням і своїм діапазоном: еталони HANDOFF §6.1 і різні межі для кожного.</summary>
    [Fact]
    public void Check_AnyStyle_MapsEachParameterToOwnValueAndRange()
    {
        var crystal = new Malt { Name = "Crystal 60", Amount = 0.3, YieldPercent = 0, ColorLovibond = 60 };
        var pilsnerColorOnly = new Malt { Name = "Pilsner", Amount = 5, YieldPercent = 0, ColorLovibond = 1.8 };
        var hop = BoilHop();
        var stats = RecipeCalculator.Calculate(RecipeOf(23, Sucrose(kilograms: 2.9824), pilsnerColorOnly, crystal, hop, Us05(75)));
        var style = new BeerStyle
        {
            OgRange = new ValueRange(1.040, 1.041),
            FgRange = new ValueRange(1.010, 1.011),
            AbvRange = new ValueRange(4, 5),
            IbuRange = new ValueRange(50, 60),
            SrmRange = new ValueRange(7, 8),
        };

        var checks = StyleCheck.Check(style, stats);

        Assert.Equal(["OG", "FG", "ABV", "IBU", "SRM"], checks.Select(check => check.Parameter));
        Assert.Equal(1.050, checks[0].Value, 0.001);
        Assert.Equal(1.0125, checks[1].Value, 0.001);
        Assert.Equal(4.92, checks[2].Value, 0.01);
        Assert.Equal(30.1, checks[3].Value, 0.2);
        Assert.Equal(7.14, checks[4].Value, 0.05);
        Assert.Equal(style.OgRange, checks[0].Range);
        Assert.Equal(style.FgRange, checks[1].Range);
        Assert.Equal(style.AbvRange, checks[2].Range);
        Assert.Equal(style.IbuRange, checks[3].Range);
        Assert.Equal(style.SrmRange, checks[4].Range);
        Assert.Equal(StyleCheckStatus.Above, checks[0].Status);
        Assert.Equal(StyleCheckStatus.Above, checks[1].Status);
        Assert.Equal(StyleCheckStatus.Within, checks[2].Status);
        Assert.Equal(StyleCheckStatus.Below, checks[3].Status);
        Assert.Equal(StyleCheckStatus.Within, checks[4].Status);
    }
}
