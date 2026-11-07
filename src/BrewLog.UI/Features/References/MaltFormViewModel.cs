using BrewLog.Domain.Recipes;
using BrewLog.UI.Features.Common;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.References;

/// <summary>Характеристики солоду: вихід екстракту, колір і вид («зерно» чи «екстракт»).</summary>
public sealed partial class MaltFormViewModel : IngredientFormViewModel
{
    private readonly Malt _malt;

    public MaltFormViewModel(Malt malt, Action changed) : base(malt, changed)
    {
        _malt = malt;
        YieldPercent = Field(malt.YieldPercent, value => malt.YieldPercent = value);
        ColorLovibond = Field(malt.ColorLovibond, value => malt.ColorLovibond = value);
    }

    public NumberField YieldPercent { get; }

    public NumberField ColorLovibond { get; }

    public bool IsGrain => _malt.IsMashed;

    public bool IsExtract => !_malt.IsMashed;

    [RelayCommand]
    private void UseGrain() => SetMashed(true);

    [RelayCommand]
    private void UseExtract() => SetMashed(false);

    /// <summary>
    /// Сегменти прив'язані в один бік і перемикаються командами: двостороння прив'язка пари RadioButton
    /// записала б значення в чужу форму, коли WPF перевикористовує шаблон для іншого солоду.
    /// </summary>
    private void SetMashed(bool isMashed)
    {
        if (_malt.IsMashed == isMashed)
        {
            return;
        }

        _malt.IsMashed = isMashed;
        OnPropertyChanged(nameof(IsGrain));
        OnPropertyChanged(nameof(IsExtract));
        MarkChanged();
    }
}
