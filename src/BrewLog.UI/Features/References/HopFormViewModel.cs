using BrewLog.Domain.Recipes;
using BrewLog.UI.Features.Common;

namespace BrewLog.UI.Features.References;

/// <summary>Характеристика хмелю: альфа-кислоти. Час і спосіб внесення задає рядок рецепта, а не довідник.</summary>
public sealed class HopFormViewModel : IngredientFormViewModel
{
    public HopFormViewModel(Hop hop, Action changed) : base(hop, changed)
    {
        AlphaPercent = Field(hop.AlphaPercent, value => hop.AlphaPercent = value);
    }

    public NumberField AlphaPercent { get; }
}
