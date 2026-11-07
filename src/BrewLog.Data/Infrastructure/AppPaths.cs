namespace BrewLog.Data.Infrastructure;

/// <summary>Типові шляхи застосунку. Шлях до БД для тестів і smoke передається окремо, у конструктор фабрики з'єднань.</summary>
public static class AppPaths
{
    public static string DataDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BrewLog");

    public static string DefaultDatabasePath { get; } = Path.Combine(DataDirectory, "brewlog.db");

    public static string LogsDirectory { get; } = Path.Combine(DataDirectory, "logs");

    /// <summary>Файл «Запам'ятати мене»: id користувача й токен для автоматичного входу.</summary>
    public static string RememberMePath { get; } = Path.Combine(DataDirectory, "session.json");
}
