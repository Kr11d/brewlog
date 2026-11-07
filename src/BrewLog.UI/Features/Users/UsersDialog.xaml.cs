using System.Windows;

namespace BrewLog.UI.Features.Users;

/// <summary>Діалог «Користувачі». Відкривається лише через WindowService.</summary>
public partial class UsersDialog : Window
{
    private readonly UsersViewModel _viewModel;

    public UsersDialog(UsersViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        viewModel.PasswordsCleared += ClearPasswords;
    }

    /// <summary>PasswordBox не дає прив'язати пароль, тому значення передаються у ViewModel тут.</summary>
    private void OnNewPasswordChanged(object sender, RoutedEventArgs e) => _viewModel.NewPassword = NewPasswordInput.Password;

    private void OnConfirmPasswordChanged(object sender, RoutedEventArgs e) => _viewModel.ConfirmPassword = ConfirmPasswordInput.Password;

    private void ClearPasswords()
    {
        NewPasswordInput.Clear();
        ConfirmPasswordInput.Clear();
    }
}
