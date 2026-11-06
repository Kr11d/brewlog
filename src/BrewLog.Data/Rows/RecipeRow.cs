using BrewLog.Domain.Recipes;

namespace BrewLog.Data.Rows;

/// <summary>Рядок таблиці recipes.</summary>
public sealed record RecipeRow
{
    public const string InsertSql = """
        INSERT INTO recipes (name, style_id, batch_volume_l, efficiency_percent, boil_time_min, ibu_formula, notes, author_id, created_at, updated_at)
        VALUES (@Name, @StyleId, @BatchVolumeL, @EfficiencyPercent, @BoilTimeMin, @IbuFormula, @Notes, @AuthorId, @CreatedAt, @UpdatedAt);
        """;

    public const string UpdateSql = """
        UPDATE recipes
        SET name = @Name, style_id = @StyleId, batch_volume_l = @BatchVolumeL, efficiency_percent = @EfficiencyPercent,
            boil_time_min = @BoilTimeMin, ibu_formula = @IbuFormula, notes = @Notes, updated_at = @UpdatedAt
        WHERE id = @Id;
        """;

    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int? StyleId { get; init; }

    public double BatchVolumeL { get; init; }

    public double EfficiencyPercent { get; init; }

    public int BoilTimeMin { get; init; }

    public string IbuFormula { get; init; } = string.Empty;

    public string? Notes { get; init; }

    public int? AuthorId { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; init; }

    public static RecipeRow FromRecipe(Recipe recipe) => new()
    {
        Id = recipe.Id,
        Name = recipe.Name,
        StyleId = recipe.StyleId,
        BatchVolumeL = recipe.BatchVolumeL,
        EfficiencyPercent = recipe.EfficiencyPercent,
        BoilTimeMin = recipe.BoilTimeMin,
        IbuFormula = recipe.IbuFormulaCode,
        Notes = recipe.Notes,
        AuthorId = recipe.AuthorId,
        CreatedAt = recipe.CreatedAt,
        UpdatedAt = recipe.UpdatedAt,
    };

    public Recipe ToRecipe(IEnumerable<Ingredient> ingredients)
    {
        var recipe = new Recipe
        {
            Id = Id,
            Name = Name,
            StyleId = StyleId,
            BatchVolumeL = BatchVolumeL,
            EfficiencyPercent = EfficiencyPercent,
            BoilTimeMin = BoilTimeMin,
            IbuFormulaCode = IbuFormula,
            Notes = Notes,
            AuthorId = AuthorId,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };

        foreach (var ingredient in ingredients)
        {
            recipe.AddIngredient(ingredient);
        }

        return recipe;
    }
}
