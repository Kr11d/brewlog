using BrewLog.Domain.Inventory;

namespace BrewLog.Domain.Abstractions;

/// <summary>Сховище складу: партії сировини, рухи, постачальники, мінімуми й ціни позицій.</summary>
public interface IStockRepository : IRepository<StockLot>
{
    /// <summary>Усі партії із залишком.</summary>
    IReadOnlyList<StockLot> GetActiveLots();

    /// <summary>Партії позиції із залишком.</summary>
    IReadOnlyList<StockLot> GetLotsOf(int catalogId);

    /// <summary>Рухи партії в порядку запису.</summary>
    IReadOnlyList<StockMovement> GetMovements(int lotId);

    IReadOnlyList<Supplier> GetSuppliers();

    /// <summary>В одній транзакції додає партію, рух Receipt і оновлює останню ціну позиції; повертає ідентифікатор партії.</summary>
    int ReceiveLot(StockLot lot, int? userId);

    /// <summary>В одній транзакції зменшує залишки партій і пише рухи WriteOff; якщо якоїсь партії не вистачає, не змінює нічого.</summary>
    void ApplyWriteOff(IReadOnlyList<WriteOffPart> parts, int batchId, int? userId);

    void UpdateMinStock(int catalogId, double minStock);

    void UpdateLastPrice(int catalogId, decimal price);
}
