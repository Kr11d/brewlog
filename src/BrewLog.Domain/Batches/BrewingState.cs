using BrewLog.Domain.Common;

namespace BrewLog.Domain.Batches;

/// <summary>Варка; до бродіння партію пускає введена фактична OG.</summary>
public sealed class BrewingState : BatchState
{
    public override string Code => "Brewing";

    public override string DisplayName => "Варка";

    public override int Order => 2;

    public override bool AllowsActualOg => true;

    public override TransitionCheck CanAdvance(TransitionContext ctx) =>
        ctx.Batch.ActualOg.HasValue
            ? TransitionCheck.Ok()
            : TransitionCheck.Deny("Спершу введіть фактичну початкову густину (OG).");

    public override BatchState Next() => new FermentingState();

    public override void OnEnter(Batch batch, DateTime now) => batch.BrewedAt = now;

    public override DateTime? EnteredAt(Batch batch) => batch.BrewedAt;
}
