namespace BrewLog.Domain.Reports;

/// <summary>Усі дані звіту за місяць: те, що експортується в Excel і PDF.</summary>
public sealed record MonthReport(
    MonthSummary Summary,
    IReadOnlyList<MonthlyVolume> Volumes,
    IReadOnlyList<RecipeCostRow> Costs,
    IReadOnlyList<AbvComparisonRow> AbvComparison);
