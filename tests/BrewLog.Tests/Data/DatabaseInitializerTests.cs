using Dapper;

namespace BrewLog.Tests.Data;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public void Initialize_NewDatabase_CreatesAllTablesWithForeignKeysOn()
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
        Assert.Equal(1, connection.ExecuteScalar<int>("PRAGMA foreign_keys;"));
    }
}
