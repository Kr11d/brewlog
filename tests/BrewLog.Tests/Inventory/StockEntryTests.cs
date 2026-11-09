using BrewLog.Domain.Inventory;
using BrewLog.Domain.Recipes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Inventory;

public sealed class StockEntryTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 10, 30, 0);

    /// <summary>Вручну: партія, що спливає сьогодні (3), і партія без строку (2) придатні, а вчорашня (4) — ні; разом 5.</summary>
    [Fact]
    public void Available_ExpiredLot_IsNotCounted()
    {
        var entry = EntryOf(0, Lot(4, expiresInDays: -1), Lot(3, expiresInDays: 0), Lot(2, expiresInDays: null));

        Assert.Equal(5, entry.Available(Now), 0.0001);
    }

    [Theory]
    [InlineData(10.0, 0.0, 10.0, false)]
    [InlineData(9.5, 0.0, 10.0, true)]
    [InlineData(5.0, 20.0, 10.0, true)]
    [InlineData(0.0, 0.0, 0.0, false)]
    public void IsBelowMinimum_UsableAgainstMinimum_ComparesWithoutExpired(double usable, double expired, double minStock, bool expected)
    {
        var entry = EntryOf(minStock, Lot(usable, expiresInDays: 10), Lot(expired, expiresInDays: -1));

        Assert.Equal(expected, entry.IsBelowMinimum(Now));
    }

    /// <summary>Вручну: (10·40 + 30·60) / 40 = 55; за однакової ціни — вона сама; вичерпана партія не впливає.</summary>
    [Theory]
    [InlineData(10.0, 40.0, 30.0, 60.0, 55.0)]
    [InlineData(1.0, 10.0, 3.0, 10.0, 10.0)]
    [InlineData(2.0, 30.0, 0.0, 90.0, 30.0)]
    public void AverageUnitPrice_SeveralLots_IsWeightedByQuantityLeft(double firstLeft, double firstPrice, double secondLeft, double secondPrice, double expected)
    {
        var entry = EntryOf(0, Lot(firstLeft, 10, (decimal)firstPrice), Lot(secondLeft, 10, (decimal)secondPrice));

        Assert.Equal((decimal)expected, entry.AverageUnitPrice);
    }

    private static StockEntry<Malt> EntryOf(double minStock, params StockLot[] lots) => new(Pilsner(), minStock, lots);

    private static StockLot Lot(double left, int? expiresInDays, decimal price = 40m) => new()
    {
        CatalogId = 1,
        QuantityReceived = left,
        QuantityLeft = left,
        UnitPrice = price,
        ReceivedAt = Now.AddDays(-5),
        ExpiresAt = expiresInDays is { } days ? Now.Date.AddDays(days) : null,
    };
}
