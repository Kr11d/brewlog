using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using BrewLog.Domain.Batches;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Users;
using BrewLog.UI.Features.Batches;
using BrewLog.UI.Features.Common;
using BrewLog.UI.Features.Inventory;
using BrewLog.UI.Features.RecipeEditor;
using BrewLog.UI.Features.References;
using BrewLog.UI.Features.Users;

namespace BrewLog.UI;

/// <summary>
/// Smoke-перевірка (аргумент --smoke): на тимчасовій БД створює кожне вікно й сторінку без показу.
/// Ловить помилки XAML і ресурсів, які інакше видно лише під час запуску. Код виходу: 0 — успіх, 1 — збій.
/// </summary>
public static class SmokeRunner
{
    public const string Argument = "--smoke";

    private static readonly TimeSpan WatchdogTimeout = TimeSpan.FromSeconds(60);
    private static readonly Size LayoutSize = new(1280, 800);

    /// <summary>
    /// Усе, що перевіряє smoke: кожне нове вікно додавайте сюди. Оболонка й сторінки перевіряються за кожну роль:
    /// сторінки беруться з її меню, тож нова сторінка розділу потрапляє сюди сама.
    /// </summary>
    private static IEnumerable<(string Name, Func<object> Create)> Targets(AppServices services, IReadOnlyList<User> demoUsers)
    {
        yield return ("LoginWindow", () => services.Windows.CreateLoginWindow());

        yield return ("MessageDialog: інформація", () => new MessageDialog(
            new MessageDialogViewModel(MessageKind.Info, "Заголовок", "Текст повідомлення", "Зрозуміло")));
        yield return ("MessageDialog: помилка", () => new MessageDialog(
            new MessageDialogViewModel(MessageKind.Error, "Заголовок", "Текст повідомлення", "Закрити")));
        yield return ("MessageDialog: підтвердження", () => new MessageDialog(
            new MessageDialogViewModel(MessageKind.Confirm, "Заголовок", "Текст повідомлення", "Так", "Ні")));

        foreach (var user in demoUsers)
        {
            yield return ($"{user.RoleDisplayName}: MainWindow", () => AsUser(services, user, () => services.Windows.CreateMainWindow()));

            // «Користувачі» відкриває діалог, а не сторінку.
            foreach (var section in user.Sections.Where(section => section != AppSection.Users))
            {
                yield return ($"{user.RoleDisplayName}: сторінка {section}", () => AsUser(services, user, () => CreatePage(services, section)));
            }

            // Сторінка партій відкривається дошкою: список перевіряється окремо, бо прихована таблиця не компонується.
            foreach (var section in user.Sections.Where(section => section is AppSection.Batches or AppSection.MyBatches))
            {
                yield return ($"{user.RoleDisplayName}: сторінка {section}, список", () => AsUser(services, user, () => CreateBatchesList(services, section)));
            }

            foreach (var state in BatchStateFactory.All)
            {
                yield return ($"{user.RoleDisplayName}: BatchCardWindow, {state.Code}", () => AsUser(services, user, () => services.Windows.CreateBatchCard(BatchIn(services, state).Id)));
            }

            if (user.Can(Permission.ManageBatches))
            {
                yield return ($"{user.RoleDisplayName}: NewBatchDialog", () => AsUser(services, user, () => services.Windows.CreateNewBatchDialog()));
                yield return ($"{user.RoleDisplayName}: ActualOgDialog", () => AsUser(services, user, () => services.Windows.CreateActualOgDialog(BatchIn(services, new BrewingState()).Id, 1.050)));
            }

            if (user.Can(Permission.ManageStock))
            {
                yield return ($"{user.RoleDisplayName}: StockReceiptDialog", () => AsUser(services, user, () => services.Windows.CreateStockReceiptDialog()));
            }

            if (user.Can(Permission.EditRecipes))
            {
                yield return ($"{user.RoleDisplayName}: RecipeEditorWindow, новий рецепт", () => AsUser(services, user, () => services.Windows.CreateRecipeEditor(null)));
                yield return ($"{user.RoleDisplayName}: RecipeEditorWindow, редагування", () => AsUser(services, user, () => services.Windows.CreateRecipeEditor(services.Recipes.GetAll()[0].Id)));
                yield return ($"{user.RoleDisplayName}: CatalogPicker", () => AsUser(services, user, () => CreateCatalogPicker(services)));
            }

            if (user.Can(Permission.EditReferences))
            {
                yield return ($"{user.RoleDisplayName}: ReferencesDialog, стилі", () => AsUser(services, user, () => services.Windows.CreateReferencesDialog()));
                yield return ($"{user.RoleDisplayName}: ReferencesDialog, новий стиль", () => AsUser(services, user, () => CreateNewStyleForm(services)));
                foreach (var kind in Enum.GetValues<IngredientKind>())
                {
                    yield return ($"{user.RoleDisplayName}: ReferencesDialog, {kind}", () => AsUser(services, user, () => CreateIngredientsTab(services, kind)));
                }
            }

            if (user.Can(Permission.ManageUsers))
            {
                yield return ($"{user.RoleDisplayName}: UsersDialog", () => AsUser(services, user, () => services.Windows.CreateUsersDialog()));
                yield return ($"{user.RoleDisplayName}: UsersDialog, новий користувач", () => AsUser(services, user, () => CreateNewUserForm(services)));
                yield return ($"{user.RoleDisplayName}: ResetPasswordDialog", () => AsUser(services, user, () => services.Windows.CreateResetPasswordDialog(user)));
            }
        }
    }

    private static Batch BatchIn(AppServices services, BatchState state) =>
        services.Batches.GetAll().First(batch => batch.State.Code == state.Code);

    /// <summary>Демо-даних у варці немає: партію створює той самий шлях, що й у застосунку, із плануванням і списанням сировини.</summary>
    private static void StartDemoBrew(AppServices services)
    {
        var recipe = services.Recipes.GetAll().First(item => item.Name == "Irish Stout");
        var batch = services.Batches.Plan(recipe.Id, recipe.BatchVolumeL, services.Clock.Now.Date, null);
        services.Batches.Advance(batch.Id);
    }

    private static ContentControl CreateBatchesList(AppServices services, AppSection section)
    {
        var page = CreatePage(services, section);
        ((BatchesViewModel)page.Content).IsBoardView = false;
        return page;
    }

    /// <summary>Діалог «Користувачі» у стані нового запису: так перевіряються поля паролів, приховані для наявного.</summary>
    private static UsersDialog CreateNewUserForm(AppServices services)
    {
        var dialog = services.Windows.CreateUsersDialog();
        ((UsersViewModel)dialog.DataContext).NewUserCommand.Execute(null);
        return dialog;
    }

    /// <summary>Діалог «Довідники» у стані нового стилю: лише так перевіряються порожні поля форми.</summary>
    private static ReferencesDialog CreateNewStyleForm(AppServices services)
    {
        var dialog = services.Windows.CreateReferencesDialog();
        ((ReferencesViewModel)dialog.DataContext).Styles.NewItemCommand.Execute(null);
        return dialog;
    }

    /// <summary>Вкладка інгредієнтів на потрібному типі: TabControl компонує лише вибрану вкладку, а шаблон форми в кожного типу свій.</summary>
    private static ReferencesDialog CreateIngredientsTab(AppServices services, IngredientKind kind)
    {
        var dialog = services.Windows.CreateReferencesDialog();
        var viewModel = (ReferencesViewModel)dialog.DataContext;
        viewModel.SelectedTabIndex = 1;
        viewModel.Kinds.First(section => section.Kind == kind).IsSelected = true;
        return dialog;
    }

    /// <summary>Вміст спливного вибору окремо: закритий поп-ап не компонується разом із вікном, тому шаблон списку інакше лишився б без перевірки.</summary>
    private static CatalogPicker CreateCatalogPicker(AppServices services) => new()
    {
        DataContext = new RecipeEditorViewModel(services.Recipes, services.Catalog, services.Dialogs, null).Malts,
    };

    /// <summary>Входить користувачем напряму, без форми входу, і створює вікно чи сторінку від його імені.</summary>
    private static object AsUser(AppServices services, User user, Func<object> create)
    {
        services.Session.SignIn(user);
        return create();
    }

    /// <summary>Виконує перевірку, пише лог у %TEMP%/brewlog-smoke.log і завершує процес.</summary>
    public static void Run(Application app)
    {
        // Закриття перевіреного вікна не має завершити застосунок раніше за саму перевірку.
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

        int exitCode;
        var logPath = Path.Combine(Path.GetTempPath(), "brewlog-smoke.log");
        using (var log = TextWriter.Synchronized(new StreamWriter(logPath, append: false) { AutoFlush = true }))
        using (StartWatchdog(log))
        {
            exitCode = Execute(log);
            log.WriteLine(exitCode == 0 ? "РЕЗУЛЬТАТ: успіх" : "РЕЗУЛЬТАТ: збій");
        }

        Environment.Exit(exitCode);
    }

    /// <summary>Запобіжник: зависла перевірка не має блокувати термінал чи CI.</summary>
    private static Timer StartWatchdog(TextWriter log) => new(
        _ =>
        {
            log.WriteLine($"ЗБІЙ: перевірка не завершилась за {WatchdogTimeout.TotalSeconds} с");
            Environment.Exit(1);
        },
        null,
        WatchdogTimeout,
        Timeout.InfiniteTimeSpan);

    private static int Execute(TextWriter log)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"brewlog-smoke-{Guid.NewGuid():N}.db");
        try
        {
            var services = new AppServices(databasePath, pooling: false);
            services.InitializeDatabase();
            log.WriteLine($"OK: БД ініціалізовано ({databasePath})");
            var demoUsers = SignInDemoUsers(services, log);
            LogDemoRecipes(services, log);
            StartDemoBrew(services);
            log.WriteLine("OK: планування партії й запуск варки зі списанням");

            var failures = Targets(services, demoUsers).Count(target => !TryCreate(target.Name, target.Create, log));
            return failures == 0 ? 0 : 1;
        }
        catch (Exception exception)
        {
            log.WriteLine($"ЗБІЙ: ініціалізація{Environment.NewLine}{exception}");
            return 1;
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    /// <summary>
    /// Перевіряє вхід кожного демо-користувача (HANDOFF §3.3) і пише його роль та розділи меню.
    /// Наостанок лишає в сесії пивовара: лише він бачить рецепти для діагностики нижче.
    /// </summary>
    private static IReadOnlyList<User> SignInDemoUsers(AppServices services, TextWriter log)
    {
        (string Login, string Password)[] demoAccounts = [("owner", "owner123"), ("assistant", "assistant123"), ("brewer", "brewer123")];
        var users = new List<User>();
        foreach (var (login, password) in demoAccounts)
        {
            var user = services.Auth.Login(login, password);
            services.Session.SignIn(user);
            users.Add(user);
            log.WriteLine($"OK: вхід «{login}» — {user.RoleDisplayName}, розділи: {string.Join(", ", user.Sections)}");
        }

        return users;
    }

    /// <summary>Діагностика: показники кожного демо-рецепта й відповідність його стилю.</summary>
    private static void LogDemoRecipes(AppServices services, TextWriter log)
    {
        var styles = services.Catalog.GetStyles();
        foreach (var recipe in services.Recipes.GetAll())
        {
            var stats = services.Recipes.Calculate(recipe);
            var style = styles.FirstOrDefault(item => item.Id == recipe.StyleId);
            log.WriteLine(
                $"Рецепт «{recipe.Name}»: OG {stats.Og:0.000} · FG {stats.Fg:0.000} · ABV {stats.Abv:0.0} % · " +
                $"IBU {stats.Ibu:0} · SRM {stats.Srm:0} · {DescribeStyleFit(style, stats)}");
        }
    }

    private static string DescribeStyleFit(BeerStyle? style, RecipeStats stats)
    {
        if (style is null)
        {
            return "без стилю";
        }

        var outOfRange = StyleCheck.Check(style, stats)
            .Where(check => check.Status != StyleCheckStatus.Within)
            .Select(check => check.Parameter)
            .ToList();

        return outOfRange.Count == 0
            ? $"{style.Code}: в межах стилю"
            : $"{style.Code}: поза межами стилю ({string.Join(", ", outOfRange)})";
    }

    private static bool TryCreate(string name, Func<object> create, TextWriter log)
    {
        object? created = null;
        try
        {
            created = create();
            ApplyTemplates(created);
            (created as Window)?.Close();
            log.WriteLine($"OK: {name}");
            return true;
        }
        catch (Exception exception)
        {
            log.WriteLine($"ЗБІЙ: {name}{Environment.NewLine}{exception}");
            DiscardBroken(created as Window);
            return false;
        }
    }

    /// <summary>
    /// Прибирає вміст вікна зі збоєм. Інакше WPF компонував би його знову під час наступних перевірок: той самий виняток
    /// виглядав би як збій інших вікон, а процес падав би без підсумку в лозі.
    /// </summary>
    private static void DiscardBroken(Window? window)
    {
        if (window is not null)
        {
            window.Content = null;
            window.Close();
        }
    }

    /// <summary>
    /// Шаблони елементів застосовуються лише під час компонування, тому без цього кроку
    /// відсутній ресурс чи помилка в шаблоні не проявилися б, поки вікно не покажуть.
    /// </summary>
    private static void ApplyTemplates(object created)
    {
        if (created is Window window)
        {
            new WindowInteropHelper(window).EnsureHandle();
        }

        if ((created is Window host ? host.Content : created) is FrameworkElement element)
        {
            element.Measure(LayoutSize);
            element.Arrange(new Rect(LayoutSize));
            element.UpdateLayout();
        }
    }

    private static ContentControl CreatePage(AppServices services, AppSection section)
    {
        var viewModel = services.Navigation.Navigate(section);
        if (Application.Current.TryFindResource(new DataTemplateKey(viewModel.GetType())) is null)
        {
            throw new InvalidOperationException($"Немає DataTemplate для {viewModel.GetType().Name} в App.xaml.");
        }

        return new ContentControl { Content = viewModel };
    }
}
