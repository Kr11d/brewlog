using BrewLog.Domain.Inventory;

namespace BrewLog.Domain.Batches;

/// <summary>Дані для перевірки переходу; Stock заповнюється лише там, де етап потребує перевірки складу.</summary>
public sealed record TransitionContext(Batch Batch, DateTime Now, StockCheckResult? Stock);
