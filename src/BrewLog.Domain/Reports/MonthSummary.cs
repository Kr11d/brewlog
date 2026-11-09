namespace BrewLog.Domain.Reports;

/// <summary>Показники місяця; середні порожні, якщо в місяці не було розливів.</summary>
public sealed record MonthSummary(int Year, int Month, int BrewedCount, double PackagedLiters, decimal? AverageCostPerLiter, double? AverageAbvDeviation);
