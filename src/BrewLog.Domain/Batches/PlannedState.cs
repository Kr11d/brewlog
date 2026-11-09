using BrewLog.Domain.Common;
using BrewLog.Domain.Inventory;

namespace BrewLog.Domain.Batches;

/// <summary>Партію заплановано; варити можна, коли на складі вистачає сировини.</summary>
public sealed class PlannedState : BatchState
{
    public override string Code => "Planned";

    public override string DisplayName => "Заплановано";

    public override int Order => 1;

    public override TransitionCheck CanAdvance(TransitionContext ctx)
    {
        if (ctx.Stock is null)
        {
            return TransitionCheck.Deny("Наявність сировини ще не перевірено.");
        }

        return ctx.Stock.IsSufficient
            ? TransitionCheck.Ok()
            : TransitionCheck.Deny(Shortage.DescribeAll(ctx.Stock.Shortages));
    }

    public override BatchState Next() => new BrewingState();

    public override DateTime? EnteredAt(Batch batch) => batch.PlannedDate;
}
