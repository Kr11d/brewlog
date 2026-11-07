using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.References;

/// <summary>Рядок-картка списку довідника: назва, підпис і необов'язковий код у чипі.</summary>
public sealed partial class ReferenceRowViewModel : ObservableObject
{
    private readonly Action<ReferenceRowViewModel> _select;

    [ObservableProperty]
    private bool _isSelected;

    public ReferenceRowViewModel(int id, string title, string caption, string code, Action<ReferenceRowViewModel> select)
    {
        Id = id;
        Title = title;
        Caption = caption;
        Code = code;
        _select = select;
    }

    public int Id { get; }

    public string Title { get; }

    public string Caption { get; }

    public string Code { get; }

    public bool HasCode => Code.Length > 0;

    /// <summary>Читає стиль ListRowCard.Selectable; позиції довідника не вимикаються, тому приглушених рядків немає.</summary>
    public bool IsMuted => false;

    [RelayCommand]
    private void Select() => _select(this);
}
