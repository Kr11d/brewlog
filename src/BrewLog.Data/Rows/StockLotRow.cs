using BrewLog.Domain.Inventory;

namespace BrewLog.Data.Rows;

/// <summary>Рядок таблиці stock_lots; ціна в БД — REAL, тому тут double, а в домені decimal.</summary>
public sealed record StockLotRow
{
    public const string InsertSql = """
        INSERT INTO stock_lots (catalog_id, supplier_id, quantity_received, quantity_left, unit_price, received_at, expires_at, lot_code)
        VALUES (@CatalogId, @SupplierId, @QuantityReceived, @QuantityLeft, @UnitPrice, @ReceivedAt, @ExpiresAt, @LotCode);
        """;

    public const string UpdateSql = """
        UPDATE stock_lots
        SET supplier_id = @SupplierId, quantity_left = @QuantityLeft, unit_price = @UnitPrice, expires_at = @ExpiresAt, lot_code = @LotCode
        WHERE id = @Id;
        """;

    public int Id { get; init; }

    public int CatalogId { get; init; }

    public int? SupplierId { get; init; }

    public double QuantityReceived { get; init; }

    public double QuantityLeft { get; init; }

    public double UnitPrice { get; init; }

    public DateTime ReceivedAt { get; init; }

    public DateTime? ExpiresAt { get; init; }

    public string? LotCode { get; init; }

    public static StockLotRow FromLot(StockLot lot) => new()
    {
        Id = lot.Id,
        CatalogId = lot.CatalogId,
        SupplierId = lot.SupplierId,
        QuantityReceived = lot.QuantityReceived,
        QuantityLeft = lot.QuantityLeft,
        UnitPrice = (double)lot.UnitPrice,
        ReceivedAt = lot.ReceivedAt,
        ExpiresAt = lot.ExpiresAt,
        LotCode = lot.LotCode,
    };

    public StockLot ToLot() => new()
    {
        Id = Id,
        CatalogId = CatalogId,
        SupplierId = SupplierId,
        QuantityReceived = QuantityReceived,
        QuantityLeft = QuantityLeft,
        UnitPrice = (decimal)UnitPrice,
        ReceivedAt = ReceivedAt,
        ExpiresAt = ExpiresAt,
        LotCode = LotCode,
    };
}
