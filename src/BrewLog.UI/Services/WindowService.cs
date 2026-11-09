using System.Windows;
using BrewLog.Domain.Common;
using BrewLog.Domain.Users;
using BrewLog.UI.Features.BatchCard;
using BrewLog.UI.Features.Batches;
using BrewLog.UI.Features.Login;
using BrewLog.UI.Features.RecipeEditor;
using BrewLog.UI.Features.References;
using BrewLog.UI.Features.Users;
using BrewLog.UI.Shell;

namespace BrewLog.UI.Services;

/// <summary>
/// Єдине місце, звідки відкриваються окремі вікна. ViewModel вікна збирається тут із сервісів AppServices.
/// Кожен розробник додає сюди методи своїх вікон, наприклад ShowBatchCard(int id), ShowBottling(int id).
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

    /// <summary>
    /// Редактор рецепта: id = null — новий рецепт. true — рецепт збережено, а savedId — його ідентифікатор:
    /// лише так сторінка може вибрати щойно створений рецепт.
    /// </summary>
    /// <exception cref="DomainException">Рецепта вже немає.</exception>
    public bool ShowRecipeEditor(int? id, out int savedId)
    {
        var window = CreateRecipeEditor(id);
        var isSaved = ModalWindow.Show(window) == true;
        savedId = window.SavedId;
        return isSaved;
    }

    /// <summary>Окремо від показу: smoke-перевірка створює вікно, не показуючи його.</summary>
    public RecipeEditorWindow CreateRecipeEditor(int? id)
    {
        var recipe = id is { } existing
            ? _services.Recipes.Get(existing) ?? throw new DomainException("Рецепт не знайдено.")
            : null;
        return new RecipeEditorWindow(new RecipeEditorViewModel(_services.Recipes, _services.Catalog, _services.Dialogs, recipe));
    }

    public void ShowReferences() => ModalWindow.Show(CreateReferencesDialog());

    public ReferencesDialog CreateReferencesDialog() => new(new ReferencesViewModel(_services.Catalog, _services.Dialogs));

    public void ShowUsers() => ModalWindow.Show(CreateUsersDialog());

    public UsersDialog CreateUsersDialog() =>
        new(new UsersViewModel(_services.Users, _services.Session, _services.Dialogs, this));

    /// <summary>Діалог нового пароля; true — пароль змінено.</summary>
    public bool ShowResetPassword(User user) => ModalWindow.Show(CreateResetPasswordDialog(user)) == true;

    public ResetPasswordDialog CreateResetPasswordDialog(User user) =>
        new(new ResetPasswordViewModel(_services.Users, _services.Dialogs, user));

    /// <exception cref="DomainException">Партії немає або немає доступу до неї.</exception>
    public void ShowBatchCard(int id) => ModalWindow.Show(CreateBatchCard(id));

    public BatchCardWindow CreateBatchCard(int id) =>
        new(new BatchCardViewModel(id, _services.Batches, _services.Session, _services.Clock, _services.Dialogs, this));

    /// <summary>Діалог фактичної OG; true — значення збережено.</summary>
    public bool ShowActualOg(int batchId, double suggestedOg) => ModalWindow.Show(CreateActualOgDialog(batchId, suggestedOg)) == true;

    public ActualOgDialog CreateActualOgDialog(int batchId, double suggestedOg) =>
        new(new ActualOgViewModel(batchId, suggestedOg, _services.Batches, _services.Dialogs));

    /// <summary>Діалог планування партії; true — партію заплановано.</summary>
    public bool ShowNewBatch() => ModalWindow.Show(CreateNewBatchDialog()) == true;

    public NewBatchDialog CreateNewBatchDialog() => new(new NewBatchViewModel(
        _services.Recipes,
        _services.Batches,
        _services.Inventory,
        _services.Users,
        _services.Clock,
        _services.Dialogs));

    private static void ShowAsMain(Window window)
    {
        Application.Current.MainWindow = window;
        window.Show();
    }
}
