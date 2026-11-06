using BrewLog.Domain.Common;

namespace BrewLog.Domain.Recipes;

/// <summary>Перевіряє, чи потрапляють показники рецепта в діапазони стилю.</summary>
public static class StyleCheck
{
    /// <summary>Повертає результат для OG, FG, ABV, IBU і SRM саме в такому порядку.</summary>
    public static IReadOnlyList<StyleParameterCheck> Check(BeerStyle style, RecipeStats stats) =>
    [
        CheckParameter("OG", stats.Og, style.OgRange),
        CheckParameter("FG", stats.Fg, style.FgRange),
        CheckParameter("ABV", stats.Abv, style.AbvRange),
        CheckParameter("IBU", stats.Ibu, style.IbuRange),
        CheckParameter("SRM", stats.Srm, style.SrmRange),
    ];

    private static StyleParameterCheck CheckParameter(string parameter, double value, ValueRange range) =>
        new(parameter, value, range, StatusOf(value, range), range.Position(value));

    private static StyleCheckStatus StatusOf(double value, ValueRange range)
    {
        if (value < range.Min)
        {
            return StyleCheckStatus.Below;
        }

        return value > range.Max ? StyleCheckStatus.Above : StyleCheckStatus.Within;
    }
}
