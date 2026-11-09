using BrewLog.Domain.Batches;

namespace BrewLog.Data.Rows;

/// <summary>Рядок таблиці batches. Об'єкт стану створює лише BatchStateFactory.</summary>
public sealed record BatchRow
{
    public const string InsertSql = """
        INSERT INTO batches
            (number, recipe_id, recipe_name, style_name, volume_l, state, planned_date, brewed_at, fermenting_at, conditioning_at, packaged_at,
             planned_og, planned_fg, planned_abv, planned_srm, temp_min_c, temp_max_c, actual_og, ingredients_cost, assistant_id, created_by, notes)
        VALUES
            (@Number, @RecipeId, @RecipeName, @StyleName, @VolumeL, @State, @PlannedDate, @BrewedAt, @FermentingAt, @ConditioningAt, @PackagedAt,
             @PlannedOg, @PlannedFg, @PlannedAbv, @PlannedSrm, @TempMinC, @TempMaxC, @ActualOg, @IngredientsCost, @AssistantId, @CreatedBy, @Notes);
        """;

    public const string UpdateStateSql = """
        UPDATE batches
        SET state = @State, brewed_at = @BrewedAt, fermenting_at = @FermentingAt, conditioning_at = @ConditioningAt, packaged_at = @PackagedAt,
            actual_og = @ActualOg, ingredients_cost = @IngredientsCost
        WHERE id = @Id;
        """;

    public const string UpdateDetailsSql = "UPDATE batches SET assistant_id = @AssistantId, notes = @Notes WHERE id = @Id;";

    public int Id { get; init; }

    public string Number { get; init; } = string.Empty;

    public int RecipeId { get; init; }

    public string RecipeName { get; init; } = string.Empty;

    public string? StyleName { get; init; }

    public double VolumeL { get; init; }

    public string State { get; init; } = string.Empty;

    public DateTime PlannedDate { get; init; }

    public DateTime? BrewedAt { get; init; }

    public DateTime? FermentingAt { get; init; }

    public DateTime? ConditioningAt { get; init; }

    public DateTime? PackagedAt { get; init; }

    public double PlannedOg { get; init; }

    public double PlannedFg { get; init; }

    public double PlannedAbv { get; init; }

    public double PlannedSrm { get; init; }

    public double? TempMinC { get; init; }

    public double? TempMaxC { get; init; }

    public double? ActualOg { get; init; }

    public double? IngredientsCost { get; init; }

    public int? AssistantId { get; init; }

    public int? CreatedBy { get; init; }

    public string? Notes { get; init; }

    public static BatchRow FromBatch(Batch batch) => new()
    {
        Id = batch.Id,
        Number = batch.Number,
        RecipeId = batch.RecipeId,
        RecipeName = batch.RecipeName,
        StyleName = batch.StyleName,
        VolumeL = batch.VolumeL,
        State = batch.State.Code,
        PlannedDate = batch.PlannedDate,
        BrewedAt = batch.BrewedAt,
        FermentingAt = batch.FermentingAt,
        ConditioningAt = batch.ConditioningAt,
        PackagedAt = batch.PackagedAt,
        PlannedOg = batch.PlannedOg,
        PlannedFg = batch.PlannedFg,
        PlannedAbv = batch.PlannedAbv,
        PlannedSrm = batch.PlannedSrm,
        TempMinC = batch.TempMinC,
        TempMaxC = batch.TempMaxC,
        ActualOg = batch.ActualOg,
        IngredientsCost = (double?)batch.IngredientsCost,
        AssistantId = batch.AssistantId,
        CreatedBy = batch.CreatedBy,
        Notes = batch.Notes,
    };

    public Batch ToBatch(IEnumerable<Measurement> measurements, Packaging? packaging) => new()
    {
        Id = Id,
        Number = Number,
        RecipeId = RecipeId,
        RecipeName = RecipeName,
        StyleName = StyleName,
        VolumeL = VolumeL,
        State = BatchStateFactory.FromCode(State),
        PlannedDate = PlannedDate,
        BrewedAt = BrewedAt,
        FermentingAt = FermentingAt,
        ConditioningAt = ConditioningAt,
        PackagedAt = PackagedAt,
        PlannedOg = PlannedOg,
        PlannedFg = PlannedFg,
        PlannedAbv = PlannedAbv,
        PlannedSrm = PlannedSrm,
        TempMinC = TempMinC,
        TempMaxC = TempMaxC,
        ActualOg = ActualOg,
        IngredientsCost = (decimal?)IngredientsCost,
        AssistantId = AssistantId,
        CreatedBy = CreatedBy,
        Notes = Notes,
        Measurements = measurements.ToList(),
        Packaging = packaging,
    };
}
