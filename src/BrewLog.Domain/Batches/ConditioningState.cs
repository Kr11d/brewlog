using BrewLog.Domain.Common;

namespace BrewLog.Domain.Batches;

/// <summary>Дозрівання; завершується оформленим розливом.</summary>
public sealed class ConditioningState : BatchState
{
    public override string Code => "Conditioning";

    public override string DisplayName => "Дозрівання";

    public override int Order => 4;

    public override bool AllowsMeasurements => true;

    public override bool AllowsPackaging => true;

    public override TransitionCheck CanAdvance(TransitionContext ctx) =>
        ctx.Batch.Packaging is not null
            ? TransitionCheck.Ok()
            : TransitionCheck.Deny("Спершу оформіть розлив партії.");

    public override BatchState Next() => new PackagedState();

    public override void OnEnter(Batch batch, DateTime now) => batch.ConditioningAt = now;

    public override DateTime? EnteredAt(Batch batch) => batch.ConditioningAt;
}
