using BrewLog.Domain.Recipes;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Рядок картки «Відповідність стилю»: назва показника, підпис «1,052 · 1,044–1,060» і дані для шкали.</summary>
public sealed record StyleRangeRow(string Parameter, string Caption, double Position, bool IsWithin)
{
    /// <summary>Формат числа для кожного показника з результату StyleCheck.</summary>
    private static readonly Dictionary<string, Func<double, string>> Formats = new()
    {
        ["OG"] = DisplayFormat.Gravity,
        ["FG"] = DisplayFormat.Gravity,
        ["ABV"] = DisplayFormat.Abv,
        ["IBU"] = DisplayFormat.Integer,
        ["SRM"] = DisplayFormat.Integer,
    };

    public static StyleRangeRow From(StyleParameterCheck check)
    {
        var format = Formats[check.Parameter];
        var caption = $"{format(check.Value)} · {format(check.Range.Min)}–{format(check.Range.Max)}";
        return new StyleRangeRow(check.Parameter, caption, check.Position, check.Status == StyleCheckStatus.Within);
    }
}
