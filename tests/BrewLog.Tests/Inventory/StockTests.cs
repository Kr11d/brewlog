using BrewLog.Domain.Inventory;
using BrewLog.Domain.Recipes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Inventory;

public sealed class StockTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 10, 30, 0);

    private const int MaltId = 1;

    [Theory]
    [InlineData(30, 5, 60, 20, 1)]
    [InlineData(60, 20, 30, 5, 2)]
    [InlineData(30, 20, 30, 5, 1)]
    [InlineData(30, 5, 30, 20, 2)]
    [InlineData(-1, 20, 60, 5, 2)]
    [InlineData(null, 20, 60, 5, 2)]
    public void PlanWriteOff_TwoLots_TakesNearestExpiryThenEarliestReceiptAndSkipsExpired(
        int? firstExpiresInDays,
        int firstReceivedDaysAgo,
        int? secondExpiresInDays,
        int secondReceivedDaysAgo,
        int expectedLotId)
    {
        var stock = StockOf(
            Lot(1, left: 5, firstExpiresInDays, firstReceivedDaysAgo),
            Lot(2, left: 5, secondExpiresInDays, secondReceivedDaysAgo));

        var parts = stock.PlanWriteOff(MaltId, 1, Now);

        Assert.Equal(expectedLotId, Assert.Single(parts).LotId);
    }

    [Fact]
    public void PlanWriteOff_MoreThanOneLotHolds_ReturnsPartPerLot()
    {
        var stock = StockOf(
            Lot(1, left: 3, expiresInDays: 10, price: 40m),
            Lot(2, left: 5, expiresInDays: 20, price: 50m));

        var parts = stock.PlanWriteOff(MaltId, 6, Now);

        Assert.Equal(2, parts.Count);
        Assert.Equal(1, parts[0].LotId);
        Assert.Equal(3, parts[0].Quantity, 0.0001);
        Assert.Equal(40m, parts[0].UnitPrice);
        Assert.Equal(2, parts[1].LotId);
        Assert.Equal(3, parts[1].Quantity, 0.0001);
        Assert.Equal(50m, parts[1].UnitPrice);
    }

    /// <summary>Прострочені 10 кг не рятують: придатних лише 1,5 кг. Числа в повідомленні завжди з десятковою комою.</summary>
    [Fact]
    public void PlanWriteOff_NotEnoughUsable_ThrowsWithUkrainianShortage()
    {
        var stock = StockOf(
            Lot(1, left: 1.5, expiresInDays: 10),
            Lot(2, left: 10, expiresInDays: -1));

        var error = Assert.Throws<InsufficientStockException>(() => stock.PlanWriteOff(MaltId, 2.5, Now));

        Assert.Contains("Pilsner: потрібно 2,5 кг, доступно 1,5 кг", error.Message);
        Assert.Equal(1.5, Assert.Single(error.Shortages).Available, 0.0001);
    }

    private static Stock<Malt> StockOf(params StockLot[] lots) =>
        new([new StockEntry<Malt>(Pilsner(), 0, lots)]);

    private static StockLot Lot(int id, double left, int? expiresInDays, int receivedDaysAgo = 1, decimal price = 40m) => new()
    {
        Id = id,
        CatalogId = MaltId,
        QuantityReceived = left,
        QuantityLeft = left,
        UnitPrice = price,
        ReceivedAt = Now.AddDays(-receivedDaysAgo),
        ExpiresAt = expiresInDays is { } days ? Now.Date.AddDays(days) : null,
    };
}
