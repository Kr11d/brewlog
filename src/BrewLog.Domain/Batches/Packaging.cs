namespace BrewLog.Domain.Batches;

/// <summary>Розлив партії: пляшки, кеги, кінцева густина. Втрати рахує Batch.SetPackaging, бо вони залежать від обсягу партії.</summary>
public sealed record Packaging
{
    public int BatchId { get; init; }

    public DateTime PackagedAt { get; init; }

    public int BottlesCount { get; init; }

    public double BottleVolumeL { get; init; } = 0.5;

    public int KegsCount { get; init; }

    public double KegVolumeL { get; init; } = 20;

    public double? FinalGravity { get; init; }

    public double LossL { get; init; }

    public int? UserId { get; init; }

    public string? Notes { get; init; }

    public double PackagedVolumeL => BottlesCount * BottleVolumeL + KegsCount * KegVolumeL;

    /// <summary>Перевіряє дані розливу. Повертає список помилок українською; порожній список означає, що розлив коректний.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (BottlesCount < 0 || KegsCount < 0)
        {
            errors.Add("Кількість пляшок і кег не може бути від'ємною.");
        }
        else if (BottlesCount == 0 && KegsCount == 0)
        {
            errors.Add("Вкажіть кількість пляшок або кег.");
        }

        if (BottlesCount > 0 && !(BottleVolumeL > 0))
        {
            errors.Add("Обсяг пляшки має бути більшим за 0.");
        }

        if (KegsCount > 0 && !(KegVolumeL > 0))
        {
            errors.Add("Обсяг кега має бути більшим за 0.");
        }

        if (FinalGravity is { } gravity && !Batch.IsMeasurableGravity(gravity))
        {
            errors.Add("Кінцева густина має бути в межах 0,990–1,150.");
        }

        return errors;
    }
}
