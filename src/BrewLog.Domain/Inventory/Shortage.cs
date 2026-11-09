namespace BrewLog.Domain.Inventory;

/// <summary>Нестача однієї позиції: скільки потрібно і скільки є придатного, в одиницях складу.</summary>
public sealed record Shortage(string Name, string Unit, double Required, double Available);
