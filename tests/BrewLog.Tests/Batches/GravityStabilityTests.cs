using BrewLog.Domain.Batches;

namespace BrewLog.Tests.Batches;

public sealed class GravityStabilityTests
{
    private static readonly DateTime Day = new(2026, 3, 10);

    public static TheoryData<Measurement[], bool> Cases => new()
    {
        { [Reading(0, 1.012), Reading(1, 1.012)], false },
        { [Reading(0, 1.012, hour: 8), Reading(0, 1.012, hour: 20), Reading(1, 1.012)], false },
        { [Reading(0, 1.012), Reading(1, 1.011), Reading(2, 1.012)], true },
        { [Reading(0, 1.012), Reading(1, 1.010), Reading(2, 1.012)], false },
        { [Reading(0, 1.030), Reading(1, 1.012), Reading(2, 1.012), Reading(3, 1.012)], true },
        { [Reading(0, 1.012), Reading(1, 1.012), TemperatureOnly(2), TemperatureOnly(3)], false },
        { [Reading(0, 1.012), Reading(1, 1.012), TemperatureOnly(1), Reading(2, 1.012), TemperatureOnly(3)], true },
        { [Reading(0, 1.012, hour: 23), Reading(1, 1.012, hour: 0), Reading(2, 1.012, hour: 1)], true },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void IsStable_LastReadings_FollowsThreeDaysRule(Measurement[] measurements, bool expected)
    {
        Assert.Equal(expected, GravityStability.IsStable(measurements));
    }

    private static Measurement Reading(int day, double gravity, int hour = 9) =>
        new() { MeasuredAt = Day.AddDays(day).AddHours(hour), Gravity = gravity };

    private static Measurement TemperatureOnly(int day) =>
        new() { MeasuredAt = Day.AddDays(day).AddHours(9), TemperatureC = 20 };
}
