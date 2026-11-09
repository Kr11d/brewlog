using BrewLog.Domain.Batches;
using BrewLog.Domain.Inventory;

namespace BrewLog.Tests.Batches;

public sealed class BatchTransitionTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 10, 30, 0);

    private static readonly StockCheckResult EnoughStock = new(true, []);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Planned_StockCheck_AllowsOnlyWhenSufficient(bool sufficient)
    {
        var batch = new Batch();
        var stock = sufficient ? EnoughStock : new StockCheckResult(false, [new Shortage("Citra", "г", 160, 120)]);

        var check = batch.State.CanAdvance(new TransitionContext(batch, Now, stock));

        Assert.Equal(sufficient, check.Allowed);
        Assert.Equal(sufficient, check.Reason is null);
        if (!sufficient)
        {
            Assert.Contains("Citra: потрібно 160 г, доступно 120 г", check.Reason);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Brewing_ActualOg_AllowsOnlyWhenEntered(bool hasOg)
    {
        var batch = new Batch { State = new BrewingState(), ActualOg = hasOg ? 1.050 : null };

        Assert.Equal(hasOg, CanAdvance(batch));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Fermenting_GravityStability_AllowsOnlyWhenStable(bool stable)
    {
        var batch = new Batch { State = new FermentingState(), Measurements = Readings(1.012, stable ? 1.012 : 1.020, 1.012) };

        Assert.Equal(stable, CanAdvance(batch));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Conditioning_Packaging_AllowsOnlyWhenRegistered(bool packaged)
    {
        var batch = new Batch { State = new ConditioningState(), Packaging = packaged ? new Packaging { BottlesCount = 10 } : null };

        Assert.Equal(packaged, CanAdvance(batch));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Packaged_AnyData_NeverAllows(bool packaged)
    {
        var batch = new Batch { State = new PackagedState(), Packaging = packaged ? new Packaging { BottlesCount = 10 } : null };

        Assert.False(CanAdvance(batch));
    }

    [Fact]
    public void Advance_AllowedTransitions_WalksStagesInOrderAndStampsEach()
    {
        var batch = new Batch { VolumeL = 50 };
        var times = Enumerable.Range(1, 4).Select(day => Now.AddDays(day)).ToArray();

        Assert.True(batch.Advance(new TransitionContext(batch, times[0], EnoughStock)).Allowed);
        Assert.Equal("Brewing", batch.State.Code);
        Assert.Equal(times[0], batch.BrewedAt);

        batch.SetActualOg(1.050);
        Assert.True(batch.Advance(new TransitionContext(batch, times[1], null)).Allowed);
        Assert.Equal("Fermenting", batch.State.Code);
        Assert.Equal(times[1], batch.FermentingAt);

        foreach (var day in new[] { 2, 3, 4 })
        {
            batch.AddMeasurement(Now.AddDays(day), 1.012, null, null, null, times[3]);
        }

        Assert.True(batch.Advance(new TransitionContext(batch, times[2], null)).Allowed);
        Assert.Equal("Conditioning", batch.State.Code);
        Assert.Equal(times[2], batch.ConditioningAt);

        batch.SetPackaging(new Packaging { BottlesCount = 100 });
        Assert.True(batch.Advance(new TransitionContext(batch, times[3], null)).Allowed);
        Assert.Equal("Packaged", batch.State.Code);
        Assert.Equal(times[3], batch.PackagedAt);

        Assert.False(batch.Advance(new TransitionContext(batch, times[3], null)).Allowed);
        Assert.Equal("Packaged", batch.State.Code);
    }

    private static bool CanAdvance(Batch batch) => batch.State.CanAdvance(new TransitionContext(batch, Now, null)).Allowed;

    private static Measurement[] Readings(params double[] gravities) =>
        gravities.Select((gravity, day) => new Measurement { MeasuredAt = Now.AddDays(day - 3), Gravity = gravity }).ToArray();
}
