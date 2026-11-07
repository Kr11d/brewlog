using BrewLog.Domain.Recipes;
using BrewLog.UI.Features.Common;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Рядок дріжджів: ступінь зброджування й температурний діапазон.</summary>
public sealed class YeastLineViewModel : IngredientLineViewModel
{
    public YeastLineViewModel(Yeast yeast, Action changed) : base(yeast, changed)
    {
        AttenuationPercent = Field(yeast.AttenuationPercent, value => yeast.AttenuationPercent = value);
        TempMinC = Field(yeast.TempMinC, value => yeast.TempMinC = value);
        TempMaxC = Field(yeast.TempMaxC, value => yeast.TempMaxC = value);
    }

    public NumberField AttenuationPercent { get; }

    public NumberField TempMinC { get; }

    public NumberField TempMaxC { get; }
}
