using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;

namespace BrewLog.Tests.Recipes;

public sealed class BeerStyleTests
{
    public static TheoryData<string, Action<BeerStyle>> InvalidStyles => new()
    {
        { "код", style => style.Code = " " },
        { "категорію", style => style.Category = "" },
        { "OG: нижня", style => style.OgRange = new ValueRange(1.070, 1.056) },
        { "OG: межі", style => style.OgRange = new ValueRange(0.950, 1.070) },
        { "OG: межі", style => style.OgRange = new ValueRange(1.056, double.NaN) },
        { "FG: межі", style => style.FgRange = new ValueRange(1.008, 1.200) },
        { "ABV: межі", style => style.AbvRange = new ValueRange(5.5, 30) },
        { "IBU: межі", style => style.IbuRange = new ValueRange(-1, 70) },
        { "SRM: межі", style => style.SrmRange = new ValueRange(6, 120) },
    };

    [Theory]
    [MemberData(nameof(InvalidStyles))]
    public void Validate_EmptyTextOrBadRange_ReportsOnlyThatError(string expected, Action<BeerStyle> spoil)
    {
        var style = new BeerStyle
        {
            Code = "21A",
            Name = "American IPA",
            Category = "IPA",
            OgRange = new ValueRange(1.056, 1.070),
            FgRange = new ValueRange(1.008, 1.014),
            AbvRange = new ValueRange(5.5, 7.5),
            IbuRange = new ValueRange(40, 70),
            SrmRange = new ValueRange(6, 14),
        };
        Assert.Empty(style.Validate());

        spoil(style);

        Assert.Contains(expected, Assert.Single(style.Validate()));
    }
}
