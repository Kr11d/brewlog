using BrewLog.Domain.Batches;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.BatchCard;

/// <summary>Рядок таблиці вимірів.</summary>
public sealed class MeasurementRowViewModel
{
    public MeasurementRowViewModel(Measurement measurement)
    {
        MeasuredAt = measurement.MeasuredAt;
        DateText = DisplayFormat.DateAndTime(measurement.MeasuredAt);
        GravityText = measurement.Gravity is { } gravity ? DisplayFormat.Gravity(gravity) : "—";
        TemperatureText = measurement.TemperatureC is { } temperature ? DisplayFormat.Temperature(temperature) : "—";
        Note = measurement.Note ?? string.Empty;
    }

    public DateTime MeasuredAt { get; }

    public string DateText { get; }

    public string GravityText { get; }

    public string TemperatureText { get; }

    public string Note { get; }
}
