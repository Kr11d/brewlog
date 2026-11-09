using BrewLog.Domain.Batches;
using BrewLog.Domain.Common;

namespace BrewLog.Tests.Batches;

public sealed class BatchTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 10, 30, 0);

    [Theory]
    [InlineData("Planned", 1.020, 20.0, 0)]
    [InlineData("Brewing", 1.020, 20.0, 0)]
    [InlineData("Packaged", 1.020, 20.0, 0)]
    [InlineData("Fermenting", null, null, 0)]
    [InlineData("Fermenting", 0.989, null, 0)]
    [InlineData("Fermenting", 1.151, null, 0)]
    [InlineData("Fermenting", null, -5.1, 0)]
    [InlineData("Fermenting", null, 40.1, 0)]
    [InlineData("Fermenting", 1.020, 20.0, 1)]
    public void AddMeasurement_WrongStageEmptyOutOfRangeOrFuture_ThrowsAndStoresNothing(string stateCode, double? gravity, double? tempC, int minutesLater)
    {
        var batch = new Batch { State = BatchStateFactory.FromCode(stateCode) };

        Assert.Throws<DomainException>(() => batch.AddMeasurement(Now.AddMinutes(minutesLater), gravity, tempC, null, null, Now));

        Assert.Empty(batch.Measurements);
    }

    /// <summary>Межі діапазонів дозволені, а вимір заднім числом стає на своє місце: поточною лишається густина за найпізнішим часом.</summary>
    [Theory]
    [InlineData("Fermenting", 0.990, -5.0)]
    [InlineData("Conditioning", 1.150, 40.0)]
    public void AddMeasurement_BoundaryValuesBackdated_StoredInDateOrder(string stateCode, double gravity, double tempC)
    {
        var batch = new Batch { State = BatchStateFactory.FromCode(stateCode) };
        batch.AddMeasurement(Now, 1.020, null, null, null, Now);

        batch.AddMeasurement(Now.AddDays(-1), gravity, tempC, null, null, Now);

        Assert.Equal(2, batch.Measurements.Count);
        Assert.Equal(Now.AddDays(-1), batch.Measurements[0].MeasuredAt);
        Assert.Equal(1.020, batch.CurrentGravity!.Value, 0.0001);
    }

    [Theory]
    [InlineData("Brewing", 1.000, true)]
    [InlineData("Brewing", 1.150, true)]
    [InlineData("Brewing", 0.999, false)]
    [InlineData("Brewing", 1.151, false)]
    [InlineData("Planned", 1.050, false)]
    [InlineData("Fermenting", 1.050, false)]
    public void SetActualOg_StageAndRange_StoresOnlyInBrewingWithinLimits(string stateCode, double og, bool accepted)
    {
        var batch = new Batch { State = BatchStateFactory.FromCode(stateCode) };

        if (accepted)
        {
            batch.SetActualOg(og);
            Assert.Equal(og, batch.ActualOg!.Value, 0.0001);
        }
        else
        {
            Assert.Throws<DomainException>(() => batch.SetActualOg(og));
            Assert.Null(batch.ActualOg);
        }
    }

    [Theory]
    [InlineData("Conditioning", 101, 0)]
    [InlineData("Conditioning", 21, 2)]
    [InlineData("Conditioning", 0, 0)]
    [InlineData("Fermenting", 10, 0)]
    public void SetPackaging_OverVolumeEmptyOrWrongStage_ThrowsAndStoresNothing(string stateCode, int bottles, int kegs)
    {
        var batch = new Batch { State = BatchStateFactory.FromCode(stateCode), VolumeL = 50 };

        Assert.Throws<DomainException>(() => batch.SetPackaging(new Packaging { BottlesCount = bottles, KegsCount = kegs }));

        Assert.Null(batch.Packaging);
    }

    /// <summary>Вручну: 40 пляшок · 0,5 + 1 кег · 20 = 40 л, втрати 10 л; 100 пляшок · 0,5 = 50 л, втрат немає.</summary>
    [Theory]
    [InlineData(40, 1, 10.0)]
    [InlineData(100, 0, 0.0)]
    public void SetPackaging_WithinVolume_ComputesLossNotBelowZero(int bottles, int kegs, double expectedLoss)
    {
        var batch = new Batch { Id = 7, State = new ConditioningState(), VolumeL = 50 };

        batch.SetPackaging(new Packaging { BottlesCount = bottles, KegsCount = kegs });

        Assert.Equal(expectedLoss, batch.Packaging!.LossL, 0.0001);
        Assert.Equal(7, batch.Packaging.BatchId);
    }

    /// <summary>Еталон HANDOFF §6.1: (1,050 − 1,0125) · 131,25 = 4,92.</summary>
    [Fact]
    public void ActualAbv_OgAndFinalGravity_MatchesReference()
    {
        var batch = new Batch { State = new ConditioningState(), VolumeL = 50, ActualOg = 1.050 };
        Assert.Null(batch.ActualAbv);

        batch.SetPackaging(new Packaging { BottlesCount = 100, FinalGravity = 1.0125 });

        Assert.Equal(4.92, batch.ActualAbv!.Value, 0.01);
    }
}
