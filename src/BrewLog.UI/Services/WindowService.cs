using System.Windows;
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

    public void ShowMainWindow()
    {
        var window = CreateMainWindow();
        Application.Current.MainWindow = window;
        window.Show();
    }

    /// <summary>Окремо від показу: smoke-перевірка створює вікно, не показуючи його.</summary>
    public MainWindow CreateMainWindow() =>
        new(new MainViewModel(_services.Session, _services.Navigation, this));

    public void ShowUsers() => _services.Dialogs.ShowInfo("Буде в B7", "Користувачі");
}
