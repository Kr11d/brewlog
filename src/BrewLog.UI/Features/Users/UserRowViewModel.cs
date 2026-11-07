using BrewLog.Domain.Users;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Users;

/// <summary>Рядок-картка списку користувачів; вимкнений обліковий запис показується приглушеним.</summary>
public sealed partial class UserRowViewModel : ObservableObject
{
    private readonly Action<UserRowViewModel> _select;

    [ObservableProperty]
    private bool _isSelected;

    public UserRowViewModel(User user, Action<UserRowViewModel> select)
    {
        User = user;
        _select = select;
    }

    public User User { get; }

    public int Id => User.Id;

    public string FullName => User.FullName;

    public string Login => User.Login;

    public string RoleDisplayName => User.RoleDisplayName;

    public bool IsMuted => !User.IsActive;

    /// <summary>Чи підходить рядок під пошук за ім'ям або логіном.</summary>
    public bool Matches(string search) =>
        FullName.Contains(search, StringComparison.CurrentCultureIgnoreCase)
        || Login.Contains(search, StringComparison.OrdinalIgnoreCase);

    [RelayCommand]
    private void Select() => _select(this);
}
