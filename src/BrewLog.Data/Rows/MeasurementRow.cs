using BrewLog.Domain.Batches;

namespace BrewLog.Data.Rows;

/// <summary>Рядок таблиці measurements.</summary>
public sealed record MeasurementRow
{
    public const string InsertSql = """
        INSERT INTO measurements (batch_id, measured_at, gravity, temperature_c, note, user_id)
        VALUES (@BatchId, @MeasuredAt, @Gravity, @TemperatureC, @Note, @UserId);
        """;

    public int Id { get; init; }

    public int BatchId { get; init; }

    public DateTime MeasuredAt { get; init; }

    public double? Gravity { get; init; }

    public double? TemperatureC { get; init; }

    public string? Note { get; init; }

    public int? UserId { get; init; }

    public static MeasurementRow FromMeasurement(Measurement measurement, int batchId) => new()
    {
        BatchId = batchId,
        MeasuredAt = measurement.MeasuredAt,
        Gravity = measurement.Gravity,
        TemperatureC = measurement.TemperatureC,
        Note = measurement.Note,
        UserId = measurement.UserId,
    };

    public Measurement ToMeasurement() => new()
    {
        Id = Id,
        BatchId = BatchId,
        MeasuredAt = MeasuredAt,
        Gravity = Gravity,
        TemperatureC = TemperatureC,
        Note = Note,
        UserId = UserId,
    };
}
