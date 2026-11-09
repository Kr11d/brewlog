using BrewLog.Domain.Batches;

namespace BrewLog.Data.Rows;

/// <summary>Рядок таблиці packagings.</summary>
public sealed record PackagingRow
{
    public const string InsertSql = """
        INSERT INTO packagings (batch_id, packaged_at, bottles_count, bottle_volume_l, kegs_count, keg_volume_l, final_gravity, loss_l, user_id, notes)
        VALUES (@BatchId, @PackagedAt, @BottlesCount, @BottleVolumeL, @KegsCount, @KegVolumeL, @FinalGravity, @LossL, @UserId, @Notes);
        """;

    public int Id { get; init; }

    public int BatchId { get; init; }

    public DateTime PackagedAt { get; init; }

    public int BottlesCount { get; init; }

    public double BottleVolumeL { get; init; }

    public int KegsCount { get; init; }

    public double KegVolumeL { get; init; }

    public double? FinalGravity { get; init; }

    public double LossL { get; init; }

    public int? UserId { get; init; }

    public string? Notes { get; init; }

    public static PackagingRow FromPackaging(Packaging packaging, int batchId) => new()
    {
        BatchId = batchId,
        PackagedAt = packaging.PackagedAt,
        BottlesCount = packaging.BottlesCount,
        BottleVolumeL = packaging.BottleVolumeL,
        KegsCount = packaging.KegsCount,
        KegVolumeL = packaging.KegVolumeL,
        FinalGravity = packaging.FinalGravity,
        LossL = packaging.LossL,
        UserId = packaging.UserId,
        Notes = packaging.Notes,
    };

    public Packaging ToPackaging() => new()
    {
        BatchId = BatchId,
        PackagedAt = PackagedAt,
        BottlesCount = BottlesCount,
        BottleVolumeL = BottleVolumeL,
        KegsCount = KegsCount,
        KegVolumeL = KegVolumeL,
        FinalGravity = FinalGravity,
        LossL = LossL,
        UserId = UserId,
        Notes = Notes,
    };
}
