namespace BrewLog.Domain.Abstractions;

/// <summary>Реальний годинник для робочого застосунку.</summary>
public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}
