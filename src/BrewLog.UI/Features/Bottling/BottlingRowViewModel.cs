using BrewLog.Domain.Batches;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.Bottling;

/// <summary>Партія, готова до розливу, у списку сторінки.</summary>
public sealed class BottlingRowViewModel
{
    public BottlingRowViewModel(Batch batch, DateTime now)
    {
        Id = batch.Id;
        Number = batch.Number;
        RecipeName = batch.RecipeName;
        VolumeText = DisplayFormat.Amount(batch.VolumeL, "л");
        DaysText = $"{batch.DaysInCurrentState(now)} дн. у дозріванні";
        GravityText = batch.CurrentGravity is { } gravity ? $"Густина {DisplayFormat.Gravity(gravity)}" : "Густина —";
    }

    public int Id { get; }

    public string Number { get; }

    public string RecipeName { get; }

    public string VolumeText { get; }

    public string DaysText { get; }

    public string GravityText { get; }
}
