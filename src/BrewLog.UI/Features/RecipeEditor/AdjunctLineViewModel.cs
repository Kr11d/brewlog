using BrewLog.Domain.Recipes;
using BrewLog.UI.Features.Common;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Рядок додатка: вихід екстракту.</summary>
public sealed class AdjunctLineViewModel : IngredientLineViewModel
{
    public AdjunctLineViewModel(Adjunct adjunct, Action changed) : base(adjunct, changed)
    {
        YieldPercent = Field(adjunct.YieldPercent, value => adjunct.YieldPercent = value);
    }

    public NumberField YieldPercent { get; }
}
