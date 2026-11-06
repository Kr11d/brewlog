using BrewLog.Data.Infrastructure;
using BrewLog.Data.Seed;

namespace BrewLog.Tests.Data;

/// <summary>
/// Власна тимчасова БД для одного тесту: xUnit запускає класи паралельно, тому спільний файл не підходить.
/// Без пулу з'єднань, інакше файл не вдалося б видалити в Dispose.
/// </summary>
public sealed class TempDatabase : IDisposable
{
    public TempDatabase(IEnumerable<ISeeder>? seeders = null)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"brewlog-test-{Guid.NewGuid():N}.db");
        Connections = new SqliteConnectionFactory(Path, pooling: false);
        new DatabaseInitializer(Connections, seeders ?? []).Initialize(Today);
    }

    public string Path { get; }

    public SqliteConnectionFactory Connections { get; }

    /// <summary>Фіксована дата, від якої сідери рахують демо-дані.</summary>
    public DateTime Today { get; } = new(2026, 3, 15);

    public void Dispose() => File.Delete(Path);
}
