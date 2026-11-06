using Dapper;

namespace BrewLog.Tests.Data;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public void Initialize_NewDatabase_CreatesAllTables()
    {
        using var database = new TempDatabase();
        using var connection = database.Connections.Open();

        var tables = connection.Query<string>(
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite%' ORDER BY name");

        string[] expected =
        [
            "batches",
            "ingredient_catalog",
            "measurements",
            "packagings",
            "recipe_ingredients",
            "recipes",
            "stock_lots",
            "stock_movements",
            "styles",
            "suppliers",
            "users",
        ];
        Assert.Equal(expected, tables);
    }

    [Fact]
    public void Open_NewConnection_EnablesForeignKeys()
    {
        using var database = new TempDatabase();
        using var connection = database.Connections.Open();

        var foreignKeys = connection.ExecuteScalar<int>("PRAGMA foreign_keys;");

        Assert.Equal(1, foreignKeys);
    }
}
