namespace BrewLog.Domain.Inventory;

/// <summary>Скільки позиції буде списано під партію; для підтвердження перед варкою.</summary>
public sealed record PlannedWriteOff(string Name, string Unit, double Quantity);
