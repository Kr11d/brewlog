using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;
using BrewLog.UI.Formatting;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrewLog.UI.Features.References;

/// <summary>Форма стилю BJCP: код, назва, категорія й п'ять діапазонів показників.</summary>
public sealed partial class StyleFormViewModel : ReferenceFormViewModel<BeerStyle>
{
    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    public StyleFormViewModel(BeerStyle style, Action changed) : base(style.Clone(), changed)
    {
        Code = style.Code;
        Name = style.Name;
        Category = style.Category;
        Ranges =
        [
            Range("OG", style.OgRange, range => Entity.OgRange = range, DisplayFormat.Gravity),
            Range("FG", style.FgRange, range => Entity.FgRange = range, DisplayFormat.Gravity),
            Range("ABV, %", style.AbvRange, range => Entity.AbvRange = range, DisplayFormat.Number),
            Range("IBU", style.IbuRange, range => Entity.IbuRange = range, DisplayFormat.Number),
            Range("SRM", style.SrmRange, range => Entity.SrmRange = range, DisplayFormat.Number),
        ];
    }

    public IReadOnlyList<StyleRangeFields> Ranges { get; }

    partial void OnCodeChanged(string value)
    {
        Entity.Code = value.Trim();
        MarkChanged();
    }

    partial void OnNameChanged(string value)
    {
        Entity.Name = value.Trim();
        MarkChanged();
    }

    partial void OnCategoryChanged(string value)
    {
        Entity.Category = value.Trim();
        MarkChanged();
    }

    /// <summary>Два поля одного діапазону: кожне змінює свою межу, а другу лишає як є.</summary>
    private StyleRangeFields Range(string label, ValueRange initial, Action<ValueRange> apply, Func<double, string> format)
    {
        var range = initial;
        return new StyleRangeFields(
            label,
            Field(initial.Min, value => apply(range = range with { Min = value }), format),
            Field(initial.Max, value => apply(range = range with { Max = value }), format));
    }
}
