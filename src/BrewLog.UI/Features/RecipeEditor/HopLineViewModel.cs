using BrewLog.Domain.Recipes;
using BrewLog.UI.Features.Common;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Рядок хмелю: альфа-кислоти, час і використання (кипіння чи сухе охмелення).</summary>
public sealed class HopLineViewModel : IngredientLineViewModel
{
    private readonly Hop _hop;
    private HopUseOption _selectedUse;

    public HopLineViewModel(Hop hop, Action changed) : base(hop, changed)
    {
        _hop = hop;
        _selectedUse = HopUseOption.All.First(option => option.Use == hop.Use);
        AlphaPercent = Field(hop.AlphaPercent, value => hop.AlphaPercent = value);
        TimeMin = Field(hop.TimeMin, value => hop.TimeMin = value);
    }

    public NumberField AlphaPercent { get; }

    public NumberField TimeMin { get; }

    public IReadOnlyList<HopUseOption> Uses => HopUseOption.All;

    public HopUseOption SelectedUse
    {
        get => _selectedUse;
        set
        {
            if (SetProperty(ref _selectedUse, value))
            {
                _hop.Use = value.Use;
                NotifyChanged();
            }
        }
    }
}
