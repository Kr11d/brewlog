namespace BrewLog.Domain.Reports;

/// <summary>Планова й фактична міцність партії, %; різниця — факт мінус план.</summary>
public sealed record AbvComparisonRow(string Number, string RecipeName, double PlannedAbv, double ActualAbv, double Difference);
