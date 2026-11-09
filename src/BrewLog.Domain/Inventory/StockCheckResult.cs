namespace BrewLog.Domain.Inventory;

/// <summary>Результат перевірки сировини для партії.</summary>
public sealed record StockCheckResult(bool IsSufficient, IReadOnlyList<Shortage> Shortages);
