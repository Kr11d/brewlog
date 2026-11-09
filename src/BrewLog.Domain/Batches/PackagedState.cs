using BrewLog.Domain.Common;

namespace BrewLog.Domain.Batches;

/// <summary>Партію розлито: фінальний етап.</summary>
public sealed class PackagedState : BatchState
{
    public override string Code => "Packaged";

    public override string DisplayName => "Розлито";

    public override int Order => 5;

    public override TransitionCheck CanAdvance(TransitionContext ctx) => TransitionCheck.Deny("Етап фінальний.");

    public override BatchState Next() => this;

    public override void OnEnter(Batch batch, DateTime now) => batch.PackagedAt = now;

    public override DateTime? EnteredAt(Batch batch) => batch.PackagedAt;
}
