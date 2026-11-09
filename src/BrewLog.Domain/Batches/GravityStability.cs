namespace BrewLog.Domain.Batches;

/// <summary>Правило «густина стабільна» (HANDOFF §6.3): бродіння завершилось, якщо густина не змінюється кілька днів поспіль.</summary>
public static class GravityStability
{
    private const int RequiredReadings = 3;
    private const double MaxSpread = 0.001 + 1e-9;

    /// <summary>
    /// Останні 3 виміри густини зроблені в 3 різні календарні дні й відрізняються не більше ніж на 0,001.
    /// Три різні дати вже охоплюють щонайменше 2 доби. Допуск 1e-9 потрібен, бо в double 1,012 − 1,011 трохи більше за 0,001.
    /// </summary>
    public static bool IsStable(IEnumerable<Measurement> measurements)
    {
        var lastReadings = measurements
            .Where(item => item.Gravity.HasValue)
            .OrderBy(item => item.MeasuredAt)
            .TakeLast(RequiredReadings)
            .ToList();

        if (lastReadings.Count < RequiredReadings
            || lastReadings.Select(item => item.MeasuredAt.Date).Distinct().Count() < RequiredReadings)
        {
            return false;
        }

        return lastReadings.Max(item => item.Gravity!.Value) - lastReadings.Min(item => item.Gravity!.Value) <= MaxSpread;
    }
}
