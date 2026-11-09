namespace BrewLog.UI.Features.BatchCard;

/// <summary>Точка графіка; IsOutOfRange позначає температуру поза діапазоном дріжджів.</summary>
public sealed record ChartPoint(DateTime At, double Value, bool IsOutOfRange = false);
