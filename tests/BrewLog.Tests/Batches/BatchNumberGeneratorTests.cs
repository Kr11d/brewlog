using BrewLog.Domain.Batches;

namespace BrewLog.Tests.Batches;

public sealed class BatchNumberGeneratorTests
{
    [Theory]
    [InlineData(2026, new string[0], "BL-2026-001")]
    [InlineData(2026, new[] { "BL-2026-001", "BL-2026-002" }, "BL-2026-003")]
    [InlineData(2026, new[] { "BL-2026-009", "BL-2026-002" }, "BL-2026-010")]
    [InlineData(2027, new[] { "BL-2026-015" }, "BL-2027-001")]
    public void Next_ExistingNumbers_ContinuesWithinYear(int year, string[] existing, string expected)
    {
        Assert.Equal(expected, BatchNumberGenerator.Next(year, existing));
    }
}
