using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Shell;

/// <summary>
/// Оболонка застосунку: меню за розділами ролі, поточна сторінка, блок профілю й вихід.
/// Стежить за сесією: після зміни користувача перебудовує меню й профіль.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly CurrentSession _session;
    private readonly AuthService _auth;
    private readonly RememberMeStore _rememberMe;
    private readonly NavigationService _navigation;
    private readonly WindowService _windows;

    private AppSection? _currentSection;

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private IReadOnlyList<MenuItemViewModel> _menuItems = [];

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _userRole = string.Empty;

    public MainViewModel(
        CurrentSession session,
        AuthService auth,
        RememberMeStore rememberMe,
        NavigationService navigation,
        WindowService windows)
    {
        _session = session;
        _auth = auth;
        _rememberMe = rememberMe;
        _navigation = navigation;
        _windows = windows;

        _session.Changed += OnSessionChanged;
        ApplySession();
    }

    /// <summary>Відписується від сесії, коли вікно закрито: сесія живе довше за оболонку.</summary>
    public void Dispose() => _session.Changed -= OnSessionChanged;

    /// <summary>Вихід: відкликає токен «Запам'ятати мене», стирає його файл, закриває сесію й повертає до вікна входу.</summary>
    [RelayCommand]
    private void SignOut()
    {
        if (_session.User is { } user)
        {
            _auth.RevokeRememberToken(user);
        }

        _rememberMe.Clear();
        _session.SignOut();
        _windows.ReturnToLogin();
    }

    private void OnSessionChanged(object? sender, EventArgs e) => ApplySession();

    /// <summary>
    /// Будує меню й профіль за поточним користувачем. Якщо відкритий розділ лишився в меню, сторінка не змінюється,
    /// інакше відкривається домашній розділ ролі.
    /// </summary>
    private void ApplySession()
    {
        var user = _session.User;
        UserName = user?.FullName ?? string.Empty;
        UserRole = user?.RoleDisplayName ?? string.Empty;
        MenuItems = _session.Sections.Select(section => new MenuItemViewModel(SectionCatalog.Get(section), OpenSection)).ToList();

        if (_currentSection is { } current && _session.Sections.Contains(current))
        {
            MarkActive(current);
        }
        else if (user is not null)
        {
            OpenSection(user.HomeSection);
        }
        else
        {
            _currentSection = null;
            CurrentPage = null;
        }
    }

    private void OpenSection(AppSection section)
    {
        // «Користувачі» — окремий діалог: сторінка й активний пункт меню не змінюються.
        if (section == AppSection.Users)
        {
            _windows.ShowUsers();
            return;
        }

        _currentSection = section;
        CurrentPage = _navigation.Navigate(section);
        MarkActive(section);
    }

    private void MarkActive(AppSection section)
    {
        foreach (var item in MenuItems)
        {
            item.IsActive = item.Section == section;
        }
    }
}
