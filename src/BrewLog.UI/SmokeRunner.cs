using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Users;
using BrewLog.UI.Features.Common;

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

    /// <summary>Усе, що перевіряє smoke: кожне нове вікно додавайте сюди. Сторінки беруться з NavigationService самі.</summary>
    private static IEnumerable<(string Name, Func<object> Create)> Targets(AppServices services)
    {
        yield return ("MainWindow", () => services.Windows.CreateMainWindow());

        yield return ("MessageDialog: інформація", () => new MessageDialog(
            new MessageDialogViewModel(MessageKind.Info, "Заголовок", "Текст повідомлення", "Зрозуміло")));
        yield return ("MessageDialog: помилка", () => new MessageDialog(
            new MessageDialogViewModel(MessageKind.Error, "Заголовок", "Текст повідомлення", "Закрити")));
        yield return ("MessageDialog: підтвердження", () => new MessageDialog(
            new MessageDialogViewModel(MessageKind.Confirm, "Заголовок", "Текст повідомлення", "Так", "Ні")));

        foreach (var section in Enum.GetValues<AppSection>())
        {
            yield return ($"Сторінка розділу {section}", () => CreatePage(services, section));
        }
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
            LogDemoRecipes(services, log);

            var failures = Targets(services).Count(target => !TryCreate(target.Name, target.Create, log));
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
        try
        {
            var created = create();
            ApplyTemplates(created);
            (created as Window)?.Close();
            log.WriteLine($"OK: {name}");
            return true;
        }
        catch (Exception exception)
        {
            log.WriteLine($"ЗБІЙ: {name}{Environment.NewLine}{exception}");
            return false;
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
