using System.Data;
using BrewLog.Data.Infrastructure;
using BrewLog.Data.Rows;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Batches;
using Dapper;

namespace BrewLog.Data.Repositories;

/// <summary>Партії разом із вимірами й розливом; кожна зміна виконується в транзакції.</summary>
public sealed class BatchRepository : Repository<Batch>, IBatchRepository
{
    public BatchRepository(SqliteConnectionFactory connections) : base(connections)
    {
    }

    protected override string TableName => "batches";

    public override IReadOnlyList<Batch> GetAll() => Load("SELECT * FROM batches ORDER BY planned_date DESC, id DESC");

    public IReadOnlyList<Batch> GetPlanned() =>
        Load("SELECT * FROM batches WHERE state = @code ORDER BY planned_date, id", new { code = new PlannedState().Code });

    public IReadOnlyList<Batch> GetForAssistant(int userId) =>
        Load("SELECT * FROM batches WHERE assistant_id = @userId OR assistant_id IS NULL ORDER BY planned_date DESC, id DESC", new { userId });

    public IReadOnlyList<string> GetNumbers() => Query<string>("SELECT number FROM batches");

    public override int Insert(Batch entity)
    {
        using var connection = Connections.Open();
        using var transaction = connection.BeginTransaction();

        entity.Id = InsertWithDetails(connection, transaction, entity);

        transaction.Commit();
        return entity.Id;
    }

    /// <summary>Оновлює все змінне в партії: етап із датами, виконавця й нотатки.</summary>
    public override void Update(Batch entity)
    {
        using var connection = Connections.Open();
        using var transaction = connection.BeginTransaction();

        var row = BatchRow.FromBatch(entity);
        connection.Execute(BatchRow.UpdateStateSql, row, transaction);
        connection.Execute(BatchRow.UpdateDetailsSql, row, transaction);

        transaction.Commit();
    }

    public int AddMeasurement(Measurement measurement)
    {
        using var connection = Connections.Open();
        using var transaction = connection.BeginTransaction();

        measurement.Id = connection.QuerySingle<int>(
            $"{MeasurementRow.InsertSql} SELECT last_insert_rowid();",
            MeasurementRow.FromMeasurement(measurement, measurement.BatchId),
            transaction);

        transaction.Commit();
        return measurement.Id;
    }

    public void SavePackaging(Batch batch)
    {
        using var connection = Connections.Open();
        using var transaction = connection.BeginTransaction();

        connection.Execute(PackagingRow.InsertSql, PackagingRow.FromPackaging(batch.Packaging!, batch.Id), transaction);
        connection.Execute(BatchRow.UpdateStateSql, BatchRow.FromBatch(batch), transaction);

        transaction.Commit();
    }

    public void UpdateState(Batch batch)
    {
        using var connection = Connections.Open();
        using var transaction = connection.BeginTransaction();

        connection.Execute(BatchRow.UpdateStateSql, BatchRow.FromBatch(batch), transaction);

        transaction.Commit();
    }

    /// <summary>Додає партію з вимірами й розливом у чужій транзакції: нею користуються і Insert, і BatchSeeder.</summary>
    internal static int InsertWithDetails(IDbConnection connection, IDbTransaction transaction, Batch batch)
    {
        var id = connection.QuerySingle<int>($"{BatchRow.InsertSql} SELECT last_insert_rowid();", BatchRow.FromBatch(batch), transaction);

        connection.Execute(MeasurementRow.InsertSql, batch.Measurements.Select(item => MeasurementRow.FromMeasurement(item, id)), transaction);
        if (batch.Packaging is { } packaging)
        {
            connection.Execute(PackagingRow.InsertSql, PackagingRow.FromPackaging(packaging, id), transaction);
        }

        return id;
    }

    protected override IReadOnlyList<Batch> Load(string sql, object? parameters = null)
    {
        using var connection = Connections.Open();

        var batches = connection.Query<BatchRow>(sql, parameters).AsList();
        var ids = batches.Select(batch => batch.Id).ToList();
        var measurements = connection
            .Query<MeasurementRow>("SELECT * FROM measurements WHERE batch_id IN @ids ORDER BY measured_at, id", new { ids })
            .ToLookup(row => row.BatchId);
        var packagings = connection
            .Query<PackagingRow>("SELECT * FROM packagings WHERE batch_id IN @ids", new { ids })
            .ToDictionary(row => row.BatchId);

        return batches
            .Select(batch => batch.ToBatch(
                measurements[batch.Id].Select(row => row.ToMeasurement()),
                packagings.GetValueOrDefault(batch.Id)?.ToPackaging()))
            .ToList();
    }
}
