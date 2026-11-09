using BrewLog.Domain.Inventory;

namespace BrewLog.Data.Rows;

/// <summary>Рядок таблиці stock_movements.</summary>
public sealed record StockMovementRow
{
    public const string InsertSql = """
        INSERT INTO stock_movements (lot_id, batch_id, kind, quantity, unit_price, created_at, user_id)
        VALUES (@LotId, @BatchId, @Kind, @Quantity, @UnitPrice, @CreatedAt, @UserId);
        """;

    public int Id { get; init; }

    public int LotId { get; init; }

    public int? BatchId { get; init; }

    public string Kind { get; init; } = string.Empty;

    public double Quantity { get; init; }

    public double UnitPrice { get; init; }

    public DateTime CreatedAt { get; init; }

    public int? UserId { get; init; }

    public StockMovement ToMovement() => new()
    {
        Id = Id,
        LotId = LotId,
        BatchId = BatchId,
        Kind = Enum.Parse<StockMovementKind>(Kind),
        Quantity = Quantity,
        UnitPrice = (decimal)UnitPrice,
        CreatedAt = CreatedAt,
        UserId = UserId,
    };
}
