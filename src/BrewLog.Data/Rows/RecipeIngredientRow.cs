using BrewLog.Domain.Recipes;

namespace BrewLog.Data.Rows;

/// <summary>Рядок таблиці recipe_ingredients. Об'єкт потрібного класу створює лише IngredientFactory.</summary>
public sealed record RecipeIngredientRow
{
    public const string InsertSql = """
        INSERT INTO recipe_ingredients
            (recipe_id, catalog_id, kind, name, amount, yield_percent, color_lovibond, is_mashed,
             alpha_percent, hop_time_min, hop_use, attenuation_percent, temp_min_c, temp_max_c, sort_order)
        VALUES
            (@RecipeId, @CatalogId, @Kind, @Name, @Amount, @YieldPercent, @ColorLovibond, @IsMashed,
             @AlphaPercent, @HopTimeMin, @HopUse, @AttenuationPercent, @TempMinC, @TempMaxC, @SortOrder);
        """;

    public int Id { get; init; }

    public int RecipeId { get; init; }

    public int CatalogId { get; init; }

    public string Kind { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public double Amount { get; init; }

    public double? YieldPercent { get; init; }

    public double? ColorLovibond { get; init; }

    public int? IsMashed { get; init; }

    public double? AlphaPercent { get; init; }

    public double? HopTimeMin { get; init; }

    public string? HopUse { get; init; }

    public double? AttenuationPercent { get; init; }

    public double? TempMinC { get; init; }

    public double? TempMaxC { get; init; }

    public int SortOrder { get; init; }

    public static RecipeIngredientRow FromData(IngredientData data, int recipeId, int sortOrder) => new()
    {
        Id = data.Id,
        RecipeId = recipeId,
        CatalogId = data.CatalogId,
        Kind = data.Kind.ToString(),
        Name = data.Name,
        Amount = data.Amount,
        YieldPercent = data.YieldPercent,
        ColorLovibond = data.ColorLovibond,
        IsMashed = data.IsMashed switch { null => null, true => 1, false => 0 },
        AlphaPercent = data.AlphaPercent,
        HopTimeMin = data.TimeMin,
        HopUse = data.Use?.ToString(),
        AttenuationPercent = data.AttenuationPercent,
        TempMinC = data.TempMinC,
        TempMaxC = data.TempMaxC,
        SortOrder = sortOrder,
    };

    public IngredientData ToData() => new(Enum.Parse<IngredientKind>(Kind))
    {
        Id = Id,
        CatalogId = CatalogId,
        Name = Name,
        Amount = Amount,
        YieldPercent = YieldPercent,
        ColorLovibond = ColorLovibond,
        IsMashed = IsMashed is null ? null : IsMashed != 0,
        AlphaPercent = AlphaPercent,
        TimeMin = HopTimeMin,
        Use = HopUse is null ? null : Enum.Parse<HopUse>(HopUse),
        AttenuationPercent = AttenuationPercent,
        TempMinC = TempMinC,
        TempMaxC = TempMaxC,
    };
}
