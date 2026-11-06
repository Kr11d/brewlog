using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using BrewLog.Data.Infrastructure;
using BrewLog.UI.Shell;
using QuestPDF.Infrastructure;

namespace BrewLog.UI;

/// <summary>Точка входу: культура, ліцензія QuestPDF, перехоплення збоїв, запуск застосунку або smoke-перевірки.</summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplyUkrainianCulture();
        QuestPDF.Settings.License = LicenseType.Community;

        if (e.Args.Contains(SmokeRunner.Argument))
        {
            SmokeRunner.Run(this);
            return;
        }

        RegisterExceptionHandlers();

        var services = new AppServices(AppPaths.DefaultDatabasePath);
        services.InitializeDatabase();

        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    private static void ApplyUkrainianCulture()
    {
        var culture = CultureInfo.GetCultureInfo("uk-UA");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        // Прив'язки WPF ігнорують культуру потоку й без цього форматують числа й дати за en-US.
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }

    private void RegisterExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => ReportFailure(args.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            WriteToLog(args.Exception);
            args.SetObserved();
        };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportFailure(e.Exception);
        e.Handled = true;

        // Якщо збій стався під час запуску, без видимих вікон процес лишився б висіти у фоні.
        if (!Windows.OfType<Window>().Any(window => window.IsVisible))
        {
            Shutdown(1);
        }
    }

    private static void ReportFailure(object error)
    {
        WriteToLog(error);

        // Системне вікно лише тому, що стилізований DialogService з'явиться в B2.
        MessageBox.Show(
            $"Сталася непередбачена помилка.\n\nПодробиці записано в журнал:\n{AppPaths.LogsDirectory}",
            "BrewLog",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void WriteToLog(object error)
    {
        try
        {
            var now = DateTime.Now;
            var entry = $"[{now:yyyy-MM-dd HH:mm:ss}]{Environment.NewLine}{error}{Environment.NewLine}{Environment.NewLine}";

            Directory.CreateDirectory(AppPaths.LogsDirectory);
            File.AppendAllText(Path.Combine(AppPaths.LogsDirectory, $"brewlog-{now:yyyy-MM-dd}.log"), entry);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Збій журналу не має приховати початкову помилку: користувач усе одно побачить повідомлення.
        }
    }
}
