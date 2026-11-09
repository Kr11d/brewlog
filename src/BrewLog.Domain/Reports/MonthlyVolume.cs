namespace BrewLog.Domain.Reports;

/// <summary>Розлитий обсяг за один місяць, л.</summary>
public sealed record MonthlyVolume(int Year, int Month, double Liters);
