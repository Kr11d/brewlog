using System.Globalization;

namespace BrewLog.Domain.Batches;

/// <summary>Попередження про бродіння за правилами HANDOFF §6.4.</summary>
public static class FermentationAnalyzer
{
    private const double FinishMargin = 0.004;
    private const double Tolerance = 1e-9;
    private const double MaxDaysWithoutMeasurement = 2;

    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("uk-UA");

    /// <summary>
    /// Температуру перевіряє на етапах, де вносять виміри. Хід бродіння оцінює лише під час бродіння:
    /// після нього «готова до дозрівання» чи «немає вимірів» уже нічого не означають.
    /// </summary>
    public static IReadOnlyList<FermentationAlert> Analyze(Batch batch, DateTime now)
    {
        var alerts = new List<FermentationAlert>();
        if (!batch.State.AllowsMeasurements)
        {
            return alerts;
        }

        AddTemperatureAlert(batch, alerts);
        if (batch.State.TracksFermentation)
        {
            AddStabilityAlert(batch, alerts);
            AddMissingMeasurementAlert(batch, now, alerts);
        }

        return alerts;
    }

    private static void AddTemperatureAlert(Batch batch, List<FermentationAlert> alerts)
    {
        var temperature = batch.Measurements.LastOrDefault(item => item.TemperatureC.HasValue)?.TemperatureC;
        if (temperature is { } value && (value < batch.TempMinC || value > batch.TempMaxC))
        {
            alerts.Add(new FermentationAlert(
                AlertKind.TemperatureOutOfRange,
                Severity.Error,
                $"Температура {Format(value, "0.0")} °C поза діапазоном дріжджів {Format(batch.TempMinC, "0.#")}–{Format(batch.TempMaxC, "0.#")} °C."));
        }
    }

    private static void AddStabilityAlert(Batch batch, List<FermentationAlert> alerts)
    {
        if (batch.CurrentGravity is not { } gravity || !GravityStability.IsStable(batch.Measurements))
        {
            return;
        }

        alerts.Add(gravity > batch.PlannedFg + FinishMargin + Tolerance
            ? new FermentationAlert(
                AlertKind.Stalled,
                Severity.Warning,
                $"Бродіння зупинилось: густина {Format(gravity, "0.000")} стабільна, але вища за планову {Format(batch.PlannedFg, "0.000")}.")
            : new FermentationAlert(
                AlertKind.ReadyForConditioning,
                Severity.Info,
                $"Густина стабільна ({Format(gravity, "0.000")}): партія готова до дозрівання."));
    }

    private static void AddMissingMeasurementAlert(Batch batch, DateTime now, List<FermentationAlert> alerts)
    {
        var lastActivity = batch.Measurements.Count > 0 ? batch.Measurements[^1].MeasuredAt : batch.FermentingAt;
        if (lastActivity is { } since && (now - since).TotalDays > MaxDaysWithoutMeasurement)
        {
            alerts.Add(new FermentationAlert(
                AlertKind.NoRecentMeasurement,
                Severity.Warning,
                $"Немає вимірів понад {MaxDaysWithoutMeasurement:0} доби."));
        }
    }

    private static string Format(double? value, string format) => value?.ToString(format, DisplayCulture) ?? "—";
}
