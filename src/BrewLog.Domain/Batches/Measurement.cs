using BrewLog.Domain.Common;

namespace BrewLog.Domain.Batches;

/// <summary>Вимір партії: густина, температура або обидва значення.</summary>
public sealed class Measurement : IEntity
{
    public int Id { get; set; }

    public int BatchId { get; init; }

    public DateTime MeasuredAt { get; init; }

    public double? Gravity { get; init; }

    public double? TemperatureC { get; init; }

    public string? Note { get; init; }

    public int? UserId { get; init; }
}
