using BrewLog.Domain.Users;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Shell;

/// <summary>Пункт бічного меню.</summary>
public sealed partial class MenuItemViewModel : ObservableObject
{
    private readonly Action<AppSection> _open;

    [ObservableProperty]
    private bool _isActive;

    public MenuItemViewModel(SectionInfo info, Action<AppSection> open)
    {
        Section = info.Section;
        Title = info.Title;
        Glyph = info.Glyph;
        _open = open;
    }

    public AppSection Section { get; }

    public string Title { get; }

    public string Glyph { get; }

    [RelayCommand]
    private void Open() => _open(Section);
}
