using BrewLog.Domain.Common;

namespace BrewLog.Domain.Inventory;

/// <summary>Запис журналу складу: додатна кількість — прихід, від'ємна — списання.</summary>
public sealed class StockMovement : IEntity
{
    public int Id { get; set; }

    public int LotId { get; init; }

    public int? BatchId { get; init; }

    public StockMovementKind Kind { get; init; }

    public double Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public DateTime CreatedAt { get; init; }

    public int? UserId { get; init; }
}
