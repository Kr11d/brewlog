namespace BrewLog.Domain.Batches;

/// <summary>Вид попередження про бродіння (HANDOFF §6.4).</summary>
public enum AlertKind
{
    TemperatureOutOfRange,
    Stalled,
    ReadyForConditioning,
    NoRecentMeasurement,
}
