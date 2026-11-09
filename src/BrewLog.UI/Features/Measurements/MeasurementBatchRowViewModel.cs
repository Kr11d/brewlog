using BrewLog.Domain.Batches;
using BrewLog.UI.Features.BatchCard;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.Measurements;

/// <summary>Активна партія в журналі помічника: останні значення, час виміру й чи треба міряти сьогодні.</summary>
public sealed class MeasurementBatchRowViewModel
{
    private const int RecentCount = 5;

    public MeasurementBatchRowViewModel(Batch batch, IReadOnlyList<FermentationAlert> alerts, DateTime now)
    {
        Id = batch.Id;
        Number = batch.Number;
        RecipeName = batch.RecipeName;
        StateCode = batch.State.Code;
        StateName = batch.State.DisplayName;
        DaysText = $"{batch.DaysInCurrentState(now)} дн. на етапі";

        var gravity = batch.CurrentGravity is { } value ? DisplayFormat.Gravity(value) : "—";
        var temperature = batch.Measurements.LastOrDefault(item => item.TemperatureC.HasValue)?.TemperatureC is { } degrees
            ? DisplayFormat.Temperature(degrees)
            : "—";
        LastValuesText = $"{gravity} · {temperature}";
        LastMeasuredText = batch.Measurements.Count > 0 ? DisplayFormat.DateAndTime(batch.Measurements[^1].MeasuredAt) : "Вимірів ще немає";
        NeedsAttention = batch.Measurements.All(item => item.MeasuredAt.Date != now.Date);

        SeverityCode = alerts.Count > 0 ? alerts.Max(alert => alert.Severity).ToString() : string.Empty;
        AlertText = string.Join(Environment.NewLine, alerts.Select(alert => alert.Message));
        Recent = batch.Measurements.Reverse().Take(RecentCount).Select(item => new MeasurementRowViewModel(item)).ToList();
    }

    public int Id { get; }

    public string Number { get; }

    public string RecipeName { get; }

    public string StateCode { get; }

    public string StateName { get; }

    public string DaysText { get; }

    /// <summary>Остання густина й температура: «1,012 · 20,5 °C».</summary>
    public string LastValuesText { get; }

    public string LastMeasuredText { get; }

    /// <summary>Сьогодні партію ще не міряли: рядок підсвічено, такі партії йдуть першими.</summary>
    public bool NeedsAttention { get; }

    public string SeverityCode { get; }

    public string AlertText { get; }

    public bool HasAlert => SeverityCode.Length > 0;

    /// <summary>Останні виміри, новіші зверху.</summary>
    public IReadOnlyList<MeasurementRowViewModel> Recent { get; }

    public bool HasNoRecent => Recent.Count == 0;
}
