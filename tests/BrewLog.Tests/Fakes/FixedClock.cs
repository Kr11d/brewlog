using BrewLog.Domain.Abstractions;

namespace BrewLog.Tests.Fakes;

/// <summary>Годинник із заданою датою: час у тестах не залежить від дня запуску.</summary>
public sealed class FixedClock : IClock
{
    public FixedClock(DateTime now)
    {
        Now = now;
    }

    public DateTime Now { get; set; }
}
