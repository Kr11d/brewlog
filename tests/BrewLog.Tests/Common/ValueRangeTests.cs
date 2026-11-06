using BrewLog.Domain.Common;

namespace BrewLog.Tests.Common;

public sealed class ValueRangeTests
{
    [Theory]
    [InlineData(10.0, true)]
    [InlineData(20.0, true)]
    [InlineData(15.0, true)]
    [InlineData(9.999, false)]
    [InlineData(20.001, false)]
    public void Contains_ValueNearBounds_TreatsBoundsAsInclusive(double value, bool expected)
    {
        var range = new ValueRange(10, 20);

        Assert.Equal(expected, range.Contains(value));
    }

    [Theory]
    [InlineData(10.0, 0.0)]
    [InlineData(15.0, 0.5)]
    [InlineData(20.0, 1.0)]
    [InlineData(5.0, 0.0)]
    [InlineData(25.0, 1.0)]
    public void Position_AnyValue_ClampedToZeroOne(double value, double expected)
    {
        var range = new ValueRange(10, 20);

        Assert.Equal(expected, range.Position(value), 0.0001);
    }

    [Theory]
    [InlineData(5.0, 0.0)]
    [InlineData(10.0, 0.5)]
    [InlineData(15.0, 1.0)]
    public void Position_MinEqualsMax_DoesNotDivideByZero(double value, double expected)
    {
        var range = new ValueRange(10, 10);

        Assert.Equal(expected, range.Position(value), 0.0001);
    }
}
