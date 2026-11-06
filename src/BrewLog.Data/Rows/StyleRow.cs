using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;

namespace BrewLog.Data.Rows;

/// <summary>Рядок таблиці styles.</summary>
public sealed record StyleRow
{
    public const string InsertSql = """
        INSERT INTO styles (code, name, category, og_min, og_max, fg_min, fg_max, abv_min, abv_max, ibu_min, ibu_max, srm_min, srm_max)
        VALUES (@Code, @Name, @Category, @OgMin, @OgMax, @FgMin, @FgMax, @AbvMin, @AbvMax, @IbuMin, @IbuMax, @SrmMin, @SrmMax);
        """;

    public const string UpdateSql = """
        UPDATE styles
        SET code = @Code, name = @Name, category = @Category,
            og_min = @OgMin, og_max = @OgMax, fg_min = @FgMin, fg_max = @FgMax, abv_min = @AbvMin, abv_max = @AbvMax,
            ibu_min = @IbuMin, ibu_max = @IbuMax, srm_min = @SrmMin, srm_max = @SrmMax
        WHERE id = @Id;
        """;

    public int Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public double OgMin { get; init; }

    public double OgMax { get; init; }

    public double FgMin { get; init; }

    public double FgMax { get; init; }

    public double AbvMin { get; init; }

    public double AbvMax { get; init; }

    public double IbuMin { get; init; }

    public double IbuMax { get; init; }

    public double SrmMin { get; init; }

    public double SrmMax { get; init; }

    public static StyleRow FromStyle(BeerStyle style) => new()
    {
        Id = style.Id,
        Code = style.Code,
        Name = style.Name,
        Category = style.Category,
        OgMin = style.OgRange.Min,
        OgMax = style.OgRange.Max,
        FgMin = style.FgRange.Min,
        FgMax = style.FgRange.Max,
        AbvMin = style.AbvRange.Min,
        AbvMax = style.AbvRange.Max,
        IbuMin = style.IbuRange.Min,
        IbuMax = style.IbuRange.Max,
        SrmMin = style.SrmRange.Min,
        SrmMax = style.SrmRange.Max,
    };

    public BeerStyle ToStyle() => new()
    {
        Id = Id,
        Code = Code,
        Name = Name,
        Category = Category,
        OgRange = new ValueRange(OgMin, OgMax),
        FgRange = new ValueRange(FgMin, FgMax),
        AbvRange = new ValueRange(AbvMin, AbvMax),
        IbuRange = new ValueRange(IbuMin, IbuMax),
        SrmRange = new ValueRange(SrmMin, SrmMax),
    };
}
