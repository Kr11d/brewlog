using BrewLog.Domain.Batches;
using BrewLog.Domain.Common;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.BatchCard;

/// <summary>Знімок партії для картки: готові рядки, дані графіків і видимість дій. Після кожної зміни створюється наново.</summary>
public sealed class BatchDetailsViewModel
{
    public BatchDetailsViewModel(
        Batch batch,
        IReadOnlyList<FermentationAlert> alerts,
        TransitionCheck? advanceCheck,
        bool canManage,
        bool canMeasure,
        bool canPackage)
    {
        Number = batch.Number;
        RecipeName = batch.RecipeName;
        StyleText = batch.StyleName ?? "Без стилю";
        VolumeText = DisplayFormat.Amount(batch.VolumeL, "л");
        StateCode = batch.State.Code;
        StateName = batch.State.DisplayName;
        PlannedAbvText = DisplayFormat.Abv(batch.PlannedAbv);
        ActualAbvText = batch.ActualAbv is { } abv ? DisplayFormat.Abv(abv) : "—";
        PlannedDateText = DisplayFormat.Date(batch.PlannedDate);
        BrewedText = batch.BrewedAt is { } brewedAt ? DisplayFormat.Date(brewedAt) : "—";
        ActualOgText = batch.ActualOg is { } og ? DisplayFormat.Gravity(og) : "—";

        var last = BatchStateFactory.All.Count - 1;
        Steps = BatchStateFactory.All
            .Select((state, index) => new StageStepViewModel(
                state.Order,
                state.DisplayName,
                state.Order <= batch.State.Order && state.EnteredAt(batch) is { } enteredAt ? DisplayFormat.Date(enteredAt) : string.Empty,
                state.Order < batch.State.Order,
                state.Order == batch.State.Order,
                index > 0,
                index < last))
            .ToList();

        GravityPoints = batch.Measurements
            .Where(item => item.Gravity.HasValue)
            .Select(item => new ChartPoint(item.MeasuredAt, item.Gravity!.Value))
            .ToList();
        TemperaturePoints = batch.Measurements
            .Where(item => item.TemperatureC.HasValue)
            .Select(item => new ChartPoint(
                item.MeasuredAt,
                item.TemperatureC!.Value,
                item.TemperatureC < batch.TempMinC || item.TemperatureC > batch.TempMaxC))
            .ToList();
        PlannedFg = batch.PlannedFg;
        PlannedFgLabel = $"Планова FG {DisplayFormat.Gravity(batch.PlannedFg)}";
        TempMinC = batch.TempMinC;
        TempMaxC = batch.TempMaxC;

        Alerts = alerts;
        Measurements = batch.Measurements.Reverse().Select(item => new MeasurementRowViewModel(item)).ToList();

        ShowMeasurementForm = canMeasure && batch.State.AllowsMeasurements;
        ShowActions = canManage;
        ShowActualOg = canManage && batch.State.AllowsActualOg;
        ShowAdvance = canManage && !batch.State.IsFinal;
        ShowPackaging = canManage && canPackage && batch.State.AllowsPackaging;
        RequiresStock = batch.State.RequiresStock;
        AdvanceText = $"Перейти: {batch.State.Next().DisplayName}";
        CanAdvanceNow = advanceCheck?.Allowed ?? false;
        AdvanceReason = ShowAdvance ? advanceCheck?.Reason ?? string.Empty : string.Empty;
        PlannedOg = batch.ActualOg ?? batch.PlannedOg;
    }

    public string Number { get; }

    public string RecipeName { get; }

    public string StyleText { get; }

    public string VolumeText { get; }

    public string StateCode { get; }

    public string StateName { get; }

    public string PlannedAbvText { get; }

    public string ActualAbvText { get; }

    public string PlannedDateText { get; }

    public string BrewedText { get; }

    public string ActualOgText { get; }

    public IReadOnlyList<StageStepViewModel> Steps { get; }

    public IReadOnlyList<ChartPoint> GravityPoints { get; }

    public IReadOnlyList<ChartPoint> TemperaturePoints { get; }

    public double PlannedFg { get; }

    public string PlannedFgLabel { get; }

    public double? TempMinC { get; }

    public double? TempMaxC { get; }

    public bool HasGravity => GravityPoints.Count > 0;

    public bool HasNoGravity => !HasGravity;

    public bool HasTemperature => TemperaturePoints.Count > 0;

    public bool HasNoTemperature => !HasTemperature;

    public IReadOnlyList<FermentationAlert> Alerts { get; }

    public bool HasNoAlerts => Alerts.Count == 0;

    /// <summary>Виміри, новіші зверху.</summary>
    public IReadOnlyList<MeasurementRowViewModel> Measurements { get; }

    public bool ShowMeasurementForm { get; }

    public bool ShowActions { get; }

    public bool ShowActualOg { get; }

    public bool ShowAdvance { get; }

    public bool ShowPackaging { get; }

    public bool RequiresStock { get; }

    public string AdvanceText { get; }

    public bool CanAdvanceNow { get; }

    /// <summary>Причина, чому перейти далі зараз не можна; порожня, якщо можна.</summary>
    public string AdvanceReason { get; }

    public bool HasAdvanceReason => AdvanceReason.Length > 0;

    /// <summary>Значення, яке пропонує діалог фактичної OG.</summary>
    public double PlannedOg { get; }
}
