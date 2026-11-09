using BrewLog.Domain.Batches;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.Batches;

/// <summary>Партія для дошки й списку: готові рядки для показу. Колір етапу й рівня тема вибирає за кодами.</summary>
public sealed class BatchItemViewModel
{
    public BatchItemViewModel(Batch batch, string? assistantName, IReadOnlyList<FermentationAlert> alerts, DateTime now, bool canManage)
    {
        Id = batch.Id;
        Number = batch.Number;
        RecipeName = batch.RecipeName;
        VolumeL = batch.VolumeL;
        VolumeText = DisplayFormat.Amount(batch.VolumeL, "л");
        StateCode = batch.State.Code;
        StateName = batch.State.DisplayName;
        StateOrder = batch.State.Order;
        EnteredAt = batch.State.EnteredAt(batch) ?? batch.PlannedDate;
        DaysInState = batch.DaysInCurrentState(now);
        DaysText = $"{DaysInState} дн. на етапі";
        Gravity = batch.CurrentGravity;
        GravityText = Gravity is { } value ? DisplayFormat.Gravity(value) : "—";
        AssistantName = assistantName;
        AssistantText = assistantName ?? "—";
        IsActive = !batch.State.IsFinal;
        RequiresStock = batch.State.RequiresStock;
        CanAdvance = canManage && IsActive;
        NextStageHint = $"Перейти: {batch.State.Next().DisplayName}";
        NextStageName = batch.State.Next().DisplayName;
        SeverityCode = alerts.Count > 0 ? alerts.Max(alert => alert.Severity).ToString() : string.Empty;
        AlertText = string.Join(Environment.NewLine, alerts.Select(alert => alert.Message));
    }

    public int Id { get; }

    public string Number { get; }

    public string RecipeName { get; }

    public double VolumeL { get; }

    public string VolumeText { get; }

    public string StateCode { get; }

    public string StateName { get; }

    public int StateOrder { get; }

    /// <summary>Коли партія потрапила на поточний етап: нові зверху колонки.</summary>
    public DateTime EnteredAt { get; }

    public int DaysInState { get; }

    public string DaysText { get; }

    public double? Gravity { get; }

    public string GravityText { get; }

    public string? AssistantName { get; }

    public string AssistantText { get; }

    public bool HasAssistant => AssistantName is not null;

    /// <summary>Партія ще не на фінальному етапі.</summary>
    public bool IsActive { get; }

    public bool RequiresStock { get; }

    /// <summary>Чи показувати кнопку переходу: потрібен дозвіл і наступний етап.</summary>
    public bool CanAdvance { get; }

    public string NextStageHint { get; }

    public string NextStageName { get; }

    /// <summary>Назва найвищого рівня попереджень; порожня, якщо їх немає.</summary>
    public string SeverityCode { get; }

    public string AlertText { get; }

    public bool HasAlert => SeverityCode.Length > 0;

    public bool Matches(string search) =>
        search.Length == 0
        || Number.Contains(search, StringComparison.CurrentCultureIgnoreCase)
        || RecipeName.Contains(search, StringComparison.CurrentCultureIgnoreCase);
}
