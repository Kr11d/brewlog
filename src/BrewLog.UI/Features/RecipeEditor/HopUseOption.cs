using BrewLog.Domain.Recipes;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Варіант використання хмелю для випадного списку.</summary>
public sealed record HopUseOption(HopUse Use, string Title)
{
    public static IReadOnlyList<HopUseOption> All { get; } =
    [
        new(HopUse.Boil, "Кипіння"),
        new(HopUse.DryHop, "Сухе охмелення"),
    ];
}
