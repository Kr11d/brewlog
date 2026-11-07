using System.Windows;

namespace BrewLog.UI.Features.Users;

/// <summary>Діалог нового пароля. Відкривається лише через WindowService.</summary>
public partial class ResetPasswordDialog : Window
{
    private readonly ResetPasswordViewModel _viewModel;

    public ResetPasswordDialog(ResetPasswordViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        viewModel.CloseRequested += result => DialogResult = result;
    }

    /// <summary>PasswordBox не дає прив'язати пароль, тому значення передаються у ViewModel тут.</summary>
    private void OnPasswordChanged(object sender, RoutedEventArgs e) => _viewModel.Password = PasswordInput.Password;

    private void OnConfirmationChanged(object sender, RoutedEventArgs e) => _viewModel.Confirmation = ConfirmationInput.Password;
}
