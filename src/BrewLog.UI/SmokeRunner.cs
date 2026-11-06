using System.IO;
using System.Windows;
using BrewLog.UI.Shell;

namespace BrewLog.UI;

/// <summary>
/// Smoke-перевірка (аргумент --smoke): на тимчасовій БД створює кожне вікно й сторінку без показу.
/// Ловить помилки XAML і ресурсів, які інакше видно лише під час запуску. Код виходу: 0 — успіх, 1 — збій.
/// </summary>
public static class SmokeRunner
{
    public const string Argument = "--smoke";

    private static readonly TimeSpan WatchdogTimeout = TimeSpan.FromSeconds(60);

    /// <summary>Усе, що перевіряє smoke: кожне нове вікно чи сторінку додавайте сюди.</summary>
    private static IReadOnlyList<(string Name, Func<object> Create)> Targets(AppServices services) =>
    [
        ("MainWindow", () => new MainWindow()),
    ];

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

    private static bool TryCreate(string name, Func<object> create, TextWriter log)
    {
        try
        {
            (create() as Window)?.Close();
            log.WriteLine($"OK: {name}");
            return true;
        }
        catch (Exception exception)
        {
            log.WriteLine($"ЗБІЙ: {name}{Environment.NewLine}{exception}");
            return false;
        }
    }
}
