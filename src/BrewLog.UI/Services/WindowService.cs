using System.Windows;
using BrewLog.UI.Features.Login;
using BrewLog.UI.Shell;

namespace BrewLog.UI.Services;

/// <summary>
/// Єдине місце, звідки відкриваються окремі вікна. ViewModel вікна збирається тут із сервісів AppServices.
/// Кожен розробник додає сюди методи своїх вікон: ShowRecipeEditor(int? id), ShowBatchCard(int id), ShowBottling(int id) тощо.
/// Модальні вікна показуйте через ModalWindow.Show.
/// </summary>
public sealed class WindowService
{
    private readonly AppServices _services;

    public WindowService(AppServices services)
    {
        _services = services;
    }

    public void ShowMainWindow() => ShowAsMain(CreateMainWindow());

    /// <summary>Окремо від показу: smoke-перевірка створює вікно, не показуючи його.</summary>
    public MainWindow CreateMainWindow()
    {
        var viewModel = new MainViewModel(_services.Session, _services.Auth, _services.RememberMe, _services.Navigation, this);
        var window = new MainWindow(viewModel);
        window.Closed += (_, _) => viewModel.Dispose();
        return window;
    }

    public void ShowLoginWindow() => ShowAsMain(CreateLoginWindow());

    /// <summary>Після успішного входу вікно входу саме поступається головним вікном.</summary>
    public LoginWindow CreateLoginWindow()
    {
        var viewModel = new LoginViewModel(_services.Auth, _services.Session, _services.RememberMe);
        var window = new LoginWindow(viewModel);
        viewModel.SignedIn += () =>
        {
            ShowMainWindow();
            window.Close();
        };
        return window;
    }

    /// <summary>Після виходу: показує вхід і лише потім закриває головне вікно, щоб застосунок не лишився без вікон.</summary>
    public void ReturnToLogin()
    {
        var mainWindow = Application.Current.MainWindow;
        ShowLoginWindow();
        mainWindow?.Close();
    }

    public void ShowUsers() => _services.Dialogs.ShowInfo("Буде в B7", "Користувачі");

    private static void ShowAsMain(Window window)
    {
        Application.Current.MainWindow = window;
        window.Show();
    }
}
