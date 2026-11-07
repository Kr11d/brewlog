using System.Windows;
using System.Windows.Controls;

namespace BrewLog.UI.Features.Login;

/// <summary>Вікно входу. Відкривається лише через WindowService.</summary>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
    }

    /// <summary>PasswordBox не дає прив'язати пароль, тому передаємо його у ViewModel тут.</summary>
    private void OnPasswordChanged(object sender, RoutedEventArgs e) => _viewModel.Password = ((PasswordBox)sender).Password;
}
