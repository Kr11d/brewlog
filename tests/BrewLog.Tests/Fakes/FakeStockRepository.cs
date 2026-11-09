using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Inventory;

namespace BrewLog.Tests.Fakes;

/// <summary>Заглушка складу: тримає партії в пам'яті й запам'ятовує останнє списання.</summary>
public sealed class FakeStockRepository : InMemoryRepository<StockLot>, IStockRepository
{
    public IReadOnlyList<WriteOffPart> AppliedParts { get; private set; } = [];

    public int? AppliedBatchId { get; private set; }

    public int? AppliedUserId { get; private set; }

    /// <summary>Якщо задано, ApplyWriteOff кидає цей виняток: імітація збою списання.</summary>
    public Exception? WriteOffFailure { get; set; }

    public IReadOnlyList<StockLot> GetActiveLots() => GetAll().Where(lot => lot.QuantityLeft > 0).ToList();

    public IReadOnlyList<StockLot> GetLotsOf(int catalogId) => GetActiveLots().Where(lot => lot.CatalogId == catalogId).ToList();

    public IReadOnlyList<StockMovement> GetMovements(int lotId) => [];

    public IReadOnlyList<Supplier> GetSuppliers() => [];

    public int ReceiveLot(StockLot lot, int? userId) => Insert(lot);

    public void ApplyWriteOff(IReadOnlyList<WriteOffPart> parts, int batchId, int? userId)
    {
        if (WriteOffFailure is not null)
        {
            throw WriteOffFailure;
        }

        AppliedParts = parts;
        AppliedBatchId = batchId;
        AppliedUserId = userId;
    }

    public void UpdateMinStock(int catalogId, double minStock)
    {
    }

    public void UpdateLastPrice(int catalogId, decimal price)
    {
    }
}
