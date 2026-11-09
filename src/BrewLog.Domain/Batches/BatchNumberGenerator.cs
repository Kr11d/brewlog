using System.Globalization;

namespace BrewLog.Domain.Batches;

/// <summary>Номери партій виду BL-2026-001: нумерація окрема для кожного року.</summary>
public static class BatchNumberGenerator
{
    /// <summary>Наступний номер року: на одиницю більший за найбільший наявний, тож номер видаленої партії не повторюється всередині ряду.</summary>
    public static string Next(int year, IEnumerable<string> existingNumbers)
    {
        var prefix = $"BL-{year}-";
        var last = existingNumbers
            .Where(number => number.StartsWith(prefix, StringComparison.Ordinal))
            .Select(number => int.TryParse(number[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{last + 1:000}";
    }
}
