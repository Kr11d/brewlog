using BrewLog.Domain.Recipes;
using BrewLog.UI.Features.Common;

namespace BrewLog.UI.Features.References;

/// <summary>Характеристика додатка: вихід екстракту.</summary>
public sealed class AdjunctFormViewModel : IngredientFormViewModel
{
    public AdjunctFormViewModel(Adjunct adjunct, Action changed) : base(adjunct, changed)
    {
        YieldPercent = Field(adjunct.YieldPercent, value => adjunct.YieldPercent = value);
    }

    public NumberField YieldPercent { get; }
}
