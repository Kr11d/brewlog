using Dapper;
using Microsoft.Data.Sqlite;

namespace BrewLog.Data.Infrastructure;

/// <summary>Створює з'єднання з файлом БД. Кожне з'єднання вмикає перевірку зовнішніх ключів: у SQLite вона типово вимкнена.</summary>
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    /// <param name="databasePath">Шлях до файлу БД.</param>
    /// <param name="pooling">false для тимчасових БД: пул тримає файл відкритим, і його не вдалося б видалити.</param>
    public SqliteConnectionFactory(string databasePath, bool pooling = true)
    {
        DatabasePath = databasePath;
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = pooling,
        }.ToString();
    }

    public string DatabasePath { get; }

    /// <summary>Повертає відкрите з'єднання; закриває його той, хто викликав.</summary>
    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        connection.Execute("PRAGMA foreign_keys = ON;");
        return connection;
    }
}
