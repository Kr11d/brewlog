using System.Data;
using BrewLog.Data.Infrastructure;
using BrewLog.Data.Rows;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Recipes;
using Dapper;

namespace BrewLog.Data.Repositories;

/// <summary>Рецепти разом із рядками: рецепт і його інгредієнти завжди змінюються в одній транзакції.</summary>
public sealed class RecipeRepository : Repository<Recipe>, IRecipeRepository
{
    public RecipeRepository(SqliteConnectionFactory connections) : base(connections)
    {
    }

    protected override string TableName => "recipes";

    public override IReadOnlyList<Recipe> GetAll() => Load("SELECT * FROM recipes ORDER BY name");

    public override int Insert(Recipe entity)
    {
        using var connection = Connections.Open();
        using var transaction = connection.BeginTransaction();

        entity.Id = InsertWithLines(connection, transaction, entity);

        transaction.Commit();
        return entity.Id;
    }

    /// <summary>Оновлює рецепт і повністю перезаписує його рядки.</summary>
    public override void Update(Recipe entity)
    {
        using var connection = Connections.Open();
        using var transaction = connection.BeginTransaction();

        connection.Execute(RecipeRow.UpdateSql, RecipeRow.FromRecipe(entity), transaction);
        connection.Execute("DELETE FROM recipe_ingredients WHERE recipe_id = @id", new { id = entity.Id }, transaction);
        InsertLines(connection, transaction, entity, entity.Id);

        transaction.Commit();
    }

    public bool IsUsedInBatches(int id) => Exists("SELECT 1 FROM batches WHERE recipe_id = @id", new { id });

    /// <summary>Додає рецепт із рядками в чужій транзакції: нею користуються і Insert, і RecipeSeeder.</summary>
    internal static int InsertWithLines(IDbConnection connection, IDbTransaction transaction, Recipe recipe)
    {
        var id = connection.QuerySingle<int>(
            $"{RecipeRow.InsertSql} SELECT last_insert_rowid();",
            RecipeRow.FromRecipe(recipe),
            transaction);

        InsertLines(connection, transaction, recipe, id);
        return id;
    }

    protected override IReadOnlyList<Recipe> Load(string sql, object? parameters = null)
    {
        using var connection = Connections.Open();

        var recipes = connection.Query<RecipeRow>(sql, parameters).AsList();
        var lines = connection
            .Query<RecipeIngredientRow>(
                "SELECT * FROM recipe_ingredients WHERE recipe_id IN @ids ORDER BY sort_order, id",
                new { ids = recipes.Select(recipe => recipe.Id).ToList() })
            .ToLookup(line => line.RecipeId);

        return recipes
            .Select(recipe => recipe.ToRecipe(lines[recipe.Id].Select(line => IngredientFactory.FromData(line.ToData()))))
            .ToList();
    }

    private static void InsertLines(IDbConnection connection, IDbTransaction transaction, Recipe recipe, int recipeId)
    {
        var rows = recipe.Ingredients.Select((line, index) => RecipeIngredientRow.FromData(line.ToData(), recipeId, index));
        connection.Execute(RecipeIngredientRow.InsertSql, rows, transaction);
    }
}
