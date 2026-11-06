using BrewLog.Data.Seed;
using Dapper;
using Microsoft.Data.Sqlite;

namespace BrewLog.Data.Infrastructure;

/// <summary>Готує БД до роботи: для нового файлу створює схему й запускає сідери, наявний файл не змінює.</summary>
public sealed class DatabaseInitializer
{
    private const string SchemaResourceName = "BrewLog.Data.Infrastructure.Schema.schema.sql";

    private readonly SqliteConnectionFactory _connections;
    private readonly IReadOnlyList<ISeeder> _seeders;

    public DatabaseInitializer(SqliteConnectionFactory connections, IEnumerable<ISeeder> seeders)
    {
        _connections = connections;
        _seeders = seeders.OrderBy(seeder => seeder.Order).ToList();
    }

    /// <param name="today">Дата, від якої сідери рахують демо-дані.</param>
    public void Initialize(DateTime today)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        if (File.Exists(_connections.DatabasePath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_connections.DatabasePath))!);

        try
        {
            CreateSchemaAndSeed(today);
        }
        catch
        {
            // Порожній файл наступний запуск прийняв би за готову БД.
            SqliteConnection.ClearAllPools();
            File.Delete(_connections.DatabasePath);
            throw;
        }
    }

    private void CreateSchemaAndSeed(DateTime today)
    {
        using var connection = _connections.Open();
        using var transaction = connection.BeginTransaction();

        connection.Execute(ReadSchema(), transaction: transaction);

        foreach (var seeder in _seeders)
        {
            seeder.Seed(connection, transaction, today);
        }

        transaction.Commit();
    }

    private static string ReadSchema()
    {
        using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(SchemaResourceName)
            ?? throw new InvalidOperationException($"Не знайдено вбудований ресурс {SchemaResourceName}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
