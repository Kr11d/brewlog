using BrewLog.Domain.Recipes;
using BrewLog.UI.Features.Common;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Рядок солоду: вихід екстракту й колір.</summary>
public sealed class MaltLineViewModel : IngredientLineViewModel
{
    public MaltLineViewModel(Malt malt, Action changed) : base(malt, changed)
    {
        YieldPercent = Field(malt.YieldPercent, value => malt.YieldPercent = value);
        ColorLovibond = Field(malt.ColorLovibond, value => malt.ColorLovibond = value);
    }

    public NumberField YieldPercent { get; }

    public NumberField ColorLovibond { get; }
}
