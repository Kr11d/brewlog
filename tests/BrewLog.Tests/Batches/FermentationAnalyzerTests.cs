using BrewLog.Domain.Batches;

namespace BrewLog.Tests.Batches;

public sealed class FermentationAnalyzerTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 10, 30, 0);

    private const double PlannedFg = 1.012;

    /// <summary>Планова FG 1,012, тож межа зупинки — 1,016 включно.</summary>
    [Theory]
    [InlineData(25.0, 1.030, 1.025, 1.020, AlertKind.TemperatureOutOfRange, Severity.Error)]
    [InlineData(15.0, 1.030, 1.025, 1.020, AlertKind.TemperatureOutOfRange, Severity.Error)]
    [InlineData(20.0, 1.020, 1.020, 1.020, AlertKind.Stalled, Severity.Warning)]
    [InlineData(20.0, 1.0161, 1.0161, 1.0161, AlertKind.Stalled, Severity.Warning)]
    [InlineData(20.0, 1.016, 1.016, 1.016, AlertKind.ReadyForConditioning, Severity.Info)]
    [InlineData(20.0, 1.012, 1.012, 1.012, AlertKind.ReadyForConditioning, Severity.Info)]
    [InlineData(20.0, 1.030, 1.025, 1.020, null, null)]
    public void Analyze_LastReadings_GivesExpectedAlert(double lastTemp, double first, double second, double third, AlertKind? kind, Severity? severity)
    {
        var batch = FermentingBatch(Reading(-2, first, 20), Reading(-1, second, 20), Reading(0, third, lastTemp));

        var alerts = FermentationAnalyzer.Analyze(batch, Now);

        if (kind is null)
        {
            Assert.Empty(alerts);
        }
        else
        {
            var alert = Assert.Single(alerts);
            Assert.Equal(kind, alert.Kind);
            Assert.Equal(severity, alert.Severity);
            Assert.NotEmpty(alert.Message);
        }
    }

    /// <summary>Без вимірів відлік іде від початку бродіння, а з вимірами — від останнього; рівно 2 доби ще не привід.</summary>
    [Theory]
    [InlineData(null, 49, true)]
    [InlineData(null, 48, false)]
    [InlineData(49, 100, true)]
    [InlineData(47, 100, false)]
    public void Analyze_TimeSinceLastMeasurement_WarnsAfterTwoDays(int? lastMeasurementHoursAgo, int fermentingHoursAgo, bool expected)
    {
        var measurements = lastMeasurementHoursAgo is { } hours
            ? new[] { new Measurement { MeasuredAt = Now.AddHours(-hours), TemperatureC = 20 } }
            : [];
        var batch = FermentingBatch(measurements);
        batch.FermentingAt = Now.AddHours(-fermentingHoursAgo);

        var alerts = FermentationAnalyzer.Analyze(batch, Now);

        Assert.Equal(expected, alerts.Any(alert => alert.Kind == AlertKind.NoRecentMeasurement));
    }

    /// <summary>Після бродіння поради про його хід не показуються; температура важлива, лише поки партію міряють.</summary>
    [Theory]
    [InlineData("Conditioning", 20.0, false)]
    [InlineData("Conditioning", 25.0, true)]
    [InlineData("Packaged", 25.0, false)]
    public void Analyze_AfterFermentation_ReportsOnlyTemperatureWhileMeasured(string stateCode, double lastTemp, bool expectTemperatureAlert)
    {
        var batch = new Batch
        {
            State = BatchStateFactory.FromCode(stateCode),
            PlannedFg = PlannedFg,
            TempMinC = 18,
            TempMaxC = 22,
            FermentingAt = Now.AddDays(-20),
            Measurements = [Reading(-5, 1.012, 20), Reading(-4, 1.012, 20), Reading(-3, 1.012, lastTemp)],
        };

        var alerts = FermentationAnalyzer.Analyze(batch, Now);

        if (expectTemperatureAlert)
        {
            Assert.Equal(AlertKind.TemperatureOutOfRange, Assert.Single(alerts).Kind);
        }
        else
        {
            Assert.Empty(alerts);
        }
    }

    private static Batch FermentingBatch(params Measurement[] measurements) => new()
    {
        State = new FermentingState(),
        PlannedFg = PlannedFg,
        TempMinC = 18,
        TempMaxC = 22,
        FermentingAt = Now.AddDays(-5),
        Measurements = measurements,
    };

    private static Measurement Reading(int day, double gravity, double tempC) =>
        new() { MeasuredAt = Now.AddDays(day), Gravity = gravity, TemperatureC = tempC };
}
