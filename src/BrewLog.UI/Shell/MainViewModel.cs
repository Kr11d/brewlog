using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Users;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Shell;

/// <summary>Оболонка застосунку: меню за розділами сесії, поточна сторінка, блок профілю.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly NavigationService _navigation;
    private readonly WindowService _windows;

    [ObservableProperty]
    private object? _currentPage;

    public MainViewModel(ICurrentSession session, NavigationService navigation, WindowService windows)
    {
        _navigation = navigation;
        _windows = windows;

        UserName = session.User?.FullName ?? "Гість";
        UserRole = session.User?.RoleDisplayName ?? string.Empty;
        MenuItems = session.Sections.Select(section => new MenuItemViewModel(SectionCatalog.Get(section), OpenSection)).ToList();

        if (session.Sections.Count > 0)
        {
            OpenSection(session.Sections[0]);
        }
    }

    public IReadOnlyList<MenuItemViewModel> MenuItems { get; }

    public string UserName { get; }

    public string UserRole { get; }

    /// <summary>Вихід з'явиться в B6 разом із вікном входу.</summary>
    [RelayCommand]
    private void SignOut()
    {
    }

    private void OpenSection(AppSection section)
    {
        // «Користувачі» — окремий діалог: сторінка й активний пункт меню не змінюються.
        if (section == AppSection.Users)
        {
            _windows.ShowUsers();
            return;
        }

        CurrentPage = _navigation.Navigate(section);
        foreach (var item in MenuItems)
        {
            item.IsActive = item.Section == section;
        }
    }
}
