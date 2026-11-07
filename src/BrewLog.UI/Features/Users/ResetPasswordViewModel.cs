using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Users;

/// <summary>Маленький діалог нового пароля з підтвердженням. Лишається відкритим, доки пароль не прийнято.</summary>
public sealed partial class ResetPasswordViewModel
{
    private readonly UserService _users;
    private readonly DialogService _dialogs;
    private readonly User _user;

    public ResetPasswordViewModel(UserService users, DialogService dialogs, User user)
    {
        _users = users;
        _dialogs = dialogs;
        _user = user;
    }

    /// <summary>Просить вікно закритися; true — пароль змінено.</summary>
    public event Action<bool>? CloseRequested;

    public string Description =>
        $"Новий пароль для «{_user.FullName}». Після зміни автоматичний вхід цього користувача буде скасовано.";

    /// <summary>Значення полів приходять із PasswordBox через code-behind.</summary>
    public string Password { private get; set; } = string.Empty;

    public string Confirmation { private get; set; } = string.Empty;

    [RelayCommand]
    private void Confirm()
    {
        if (Password != Confirmation)
        {
            _dialogs.ShowError("Паролі не збігаються.");
            return;
        }

        try
        {
            _users.ResetPassword(_user.Id, Password);
            CloseRequested?.Invoke(true);
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }
    }
}
