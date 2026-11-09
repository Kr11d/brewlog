using BrewLog.Domain.Common;

namespace BrewLog.Domain.Batches;

/// <summary>Бродіння; на дозрівання партія переходить, коли густина стабільна.</summary>
public sealed class FermentingState : BatchState
{
    public override string Code => "Fermenting";

    public override string DisplayName => "Бродіння";

    public override int Order => 3;

    public override bool AllowsMeasurements => true;

    public override bool TracksFermentation => true;

    public override TransitionCheck CanAdvance(TransitionContext ctx) =>
        GravityStability.IsStable(ctx.Batch.Measurements)
            ? TransitionCheck.Ok()
            : TransitionCheck.Deny("Густина ще не стабільна: потрібні виміри за 3 різні дні з різницею не більше 0,001.");

    public override BatchState Next() => new ConditioningState();

    public override void OnEnter(Batch batch, DateTime now) => batch.FermentingAt = now;

    public override DateTime? EnteredAt(Batch batch) => batch.FermentingAt;
}
