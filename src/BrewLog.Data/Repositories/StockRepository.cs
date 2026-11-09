using System.Data;
using BrewLog.Data.Infrastructure;
using BrewLog.Data.Rows;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Inventory;
using Dapper;

namespace BrewLog.Data.Repositories;

/// <summary>Партії сировини, журнал рухів і постачальники. Зміни залишків завжди йдуть разом із записом у журнал, в одній транзакції.</summary>
public sealed class StockRepository : Repository<StockLot>, IStockRepository
{
    private readonly IClock _clock;

    public StockRepository(SqliteConnectionFactory connections, IClock clock) : base(connections)
    {
        _clock = clock;
    }

    protected override string TableName => "stock_lots";

    public IReadOnlyList<StockLot> GetActiveLots() => Load("SELECT * FROM stock_lots WHERE quantity_left > 0 ORDER BY id");

    public IReadOnlyList<StockLot> GetLotsOf(int catalogId) =>
        Load("SELECT * FROM stock_lots WHERE catalog_id = @catalogId AND quantity_left > 0 ORDER BY id", new { catalogId });

    public IReadOnlyList<StockMovement> GetMovements(int lotId) =>
        Query<StockMovementRow>("SELECT * FROM stock_movements WHERE lot_id = @lotId ORDER BY id", new { lotId })
            .Select(row => row.ToMovement())
            .ToList();

    public IReadOnlyList<Supplier> GetSuppliers() => Query<Supplier>("SELECT * FROM suppliers ORDER BY name");

    public override int Insert(StockLot entity)
    {
        entity.Id = QuerySingle<int>($"{StockLotRow.InsertSql} SELECT last_insert_rowid();", StockLotRow.FromLot(entity));
        return entity.Id;
    }

    public override void Update(StockLot entity) => Execute(StockLotRow.UpdateSql, StockLotRow.FromLot(entity));

    public int ReceiveLot(StockLot lot, int? userId)
    {
        using var connection = Connections.Open();
        using var transaction = connection.BeginTransaction();

        lot.Id = InsertReceivedLot(connection, transaction, lot, userId);
        connection.Execute(
            "UPDATE ingredient_catalog SET last_price = @price WHERE id = @catalogId;",
            new { price = (double)lot.UnitPrice, catalogId = lot.CatalogId },
            transaction);

        transaction.Commit();
        return lot.Id;
    }

    public void ApplyWriteOff(IReadOnlyList<WriteOffPart> parts, int batchId, int? userId)
    {
        using var connection = Connections.Open();
        using var transaction = connection.BeginTransaction();

        var createdAt = _clock.Now;
        foreach (var part in parts)
        {
            var changed = connection.Execute(
                """
                UPDATE stock_lots
                SET quantity_left = MAX(quantity_left - @Quantity, 0)
                WHERE id = @LotId AND quantity_left >= @Quantity - @Tolerance;
                """,
                new { part.LotId, part.Quantity, Tolerance = StockLot.QuantityTolerance },
                transaction);

            if (changed == 0)
            {
                throw new DomainException("Залишок сировини на складі змінився. Оновіть дані й спробуйте ще раз.");
            }

            InsertMovement(connection, transaction, part.LotId, batchId, StockMovementKind.WriteOff, -part.Quantity, part.UnitPrice, createdAt, userId);
        }

        transaction.Commit();
    }

    public void UpdateMinStock(int catalogId, double minStock) =>
        Execute("UPDATE ingredient_catalog SET min_stock = @minStock WHERE id = @catalogId;", new { minStock, catalogId });

    public void UpdateLastPrice(int catalogId, decimal price) =>
        Execute("UPDATE ingredient_catalog SET last_price = @price WHERE id = @catalogId;", new { price = (double)price, catalogId });

    /// <summary>Додає партію з рухом Receipt у чужій транзакції: нею користуються і ReceiveLot, і StockSeeder.</summary>
    internal static int InsertReceivedLot(IDbConnection connection, IDbTransaction transaction, StockLot lot, int? userId)
    {
        var lotId = connection.QuerySingle<int>(
            $"{StockLotRow.InsertSql} SELECT last_insert_rowid();",
            StockLotRow.FromLot(lot),
            transaction);

        InsertMovement(connection, transaction, lotId, null, StockMovementKind.Receipt, lot.QuantityReceived, lot.UnitPrice, lot.ReceivedAt, userId);
        return lotId;
    }

    protected override IReadOnlyList<StockLot> Load(string sql, object? parameters = null) =>
        Query<StockLotRow>(sql, parameters).Select(row => row.ToLot()).ToList();

    private static void InsertMovement(
        IDbConnection connection,
        IDbTransaction transaction,
        int lotId,
        int? batchId,
        StockMovementKind kind,
        double quantity,
        decimal unitPrice,
        DateTime createdAt,
        int? userId)
    {
        connection.Execute(
            StockMovementRow.InsertSql,
            new StockMovementRow
            {
                LotId = lotId,
                BatchId = batchId,
                Kind = kind.ToString(),
                Quantity = quantity,
                UnitPrice = (double)unitPrice,
                CreatedAt = createdAt,
                UserId = userId,
            },
            transaction);
    }
}
