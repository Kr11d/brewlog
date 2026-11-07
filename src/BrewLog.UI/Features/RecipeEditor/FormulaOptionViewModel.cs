using BrewLog.Domain.Ibu;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Сегмент вибору формули IBU. Перемикає RadioButton, тому вибрано завжди лише один сегмент.</summary>
public sealed class FormulaOptionViewModel : ObservableObject
{
    private readonly Action<string> _select;
    private bool _isSelected;

    public FormulaOptionViewModel(IbuFormula formula, bool isSelected, Action<string> select)
    {
        Code = formula.Code;
        DisplayName = formula.DisplayName;
        _isSelected = isSelected;
        _select = select;
    }

    public string Code { get; }

    public string DisplayName { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value) && value)
            {
                _select(Code);
            }
        }
    }
}
