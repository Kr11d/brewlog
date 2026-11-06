using BrewLog.Domain.Recipes;

namespace BrewLog.Data.Rows;

/// <summary>Рядок таблиці ingredient_catalog. Об'єкт потрібного класу створює лише IngredientFactory.</summary>
public sealed record IngredientCatalogRow
{
    public const string InsertSql = """
        INSERT INTO ingredient_catalog
            (kind, name, yield_percent, color_lovibond, is_mashed, alpha_percent, attenuation_percent, temp_min_c, temp_max_c, min_stock, last_price, notes)
        VALUES
            (@Kind, @Name, @YieldPercent, @ColorLovibond, @IsMashed, @AlphaPercent, @AttenuationPercent, @TempMinC, @TempMaxC, @MinStock, @LastPrice, @Notes);
        """;

    /// <summary>Лише назва й характеристики: мінімуми й ціни редагує власник на сторінці «Склад», а тип позиції не змінюється.</summary>
    public const string UpdateSql = """
        UPDATE ingredient_catalog
        SET name = @Name, yield_percent = @YieldPercent, color_lovibond = @ColorLovibond, is_mashed = @IsMashed,
            alpha_percent = @AlphaPercent, attenuation_percent = @AttenuationPercent, temp_min_c = @TempMinC, temp_max_c = @TempMaxC
        WHERE id = @Id;
        """;

    public int Id { get; init; }

    public string Kind { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public double? YieldPercent { get; init; }

    public double? ColorLovibond { get; init; }

    public int? IsMashed { get; init; }

    public double? AlphaPercent { get; init; }

    public double? AttenuationPercent { get; init; }

    public double? TempMinC { get; init; }

    public double? TempMaxC { get; init; }

    public double MinStock { get; init; }

    public double LastPrice { get; init; }

    public string? Notes { get; init; }

    public static IngredientCatalogRow FromData(IngredientData data, double minStock = 0, decimal lastPrice = 0) => new()
    {
        Id = data.Id,
        Kind = data.Kind.ToString(),
        Name = data.Name,
        YieldPercent = data.YieldPercent,
        ColorLovibond = data.ColorLovibond,
        IsMashed = data.IsMashed switch { null => null, true => 1, false => 0 },
        AlphaPercent = data.AlphaPercent,
        AttenuationPercent = data.AttenuationPercent,
        TempMinC = data.TempMinC,
        TempMaxC = data.TempMaxC,
        MinStock = minStock,
        LastPrice = (double)lastPrice,
    };

    public IngredientData ToData() => new(Enum.Parse<IngredientKind>(Kind))
    {
        Id = Id,
        CatalogId = Id,
        Name = Name,
        YieldPercent = YieldPercent,
        ColorLovibond = ColorLovibond,
        IsMashed = IsMashed is null ? null : IsMashed != 0,
        AlphaPercent = AlphaPercent,
        AttenuationPercent = AttenuationPercent,
        TempMinC = TempMinC,
        TempMaxC = TempMaxC,
    };
}
