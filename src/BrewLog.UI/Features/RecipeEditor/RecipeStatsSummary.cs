using BrewLog.Domain.Recipes;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Показники для «документ-картки» редактора: числа рахує RecipeService, тут вони лише форматуються.</summary>
public sealed class RecipeStatsSummary
{
    public RecipeStatsSummary(RecipeStats stats, decimal costPerLiter)
    {
        Srm = stats.Srm;
        OgText = DisplayFormat.Gravity(stats.Og);
        FgText = DisplayFormat.Gravity(stats.Fg);
        AbvText = DisplayFormat.Abv(stats.Abv);
        IbuText = DisplayFormat.Integer(stats.Ibu);
        ColorText = $"{DisplayFormat.Integer(stats.Srm)} / {DisplayFormat.Integer(stats.Ebc)}";
        CostText = $"{DisplayFormat.Money(costPerLiter)}/л";
    }

    /// <summary>Сире значення для кольорової плашки.</summary>
    public double Srm { get; }

    public string OgText { get; }

    public string FgText { get; }

    public string AbvText { get; }

    public string IbuText { get; }

    public string ColorText { get; }

    public string CostText { get; }
}
