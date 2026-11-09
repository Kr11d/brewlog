using BrewLog.Domain.Batches;
using BrewLog.Domain.Common;
using BrewLog.Domain.Inventory;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.Tests.Fakes;
using static BrewLog.Tests.Recipes.SampleIngredients;
using WarehouseInventory = BrewLog.Domain.Inventory.Inventory;

namespace BrewLog.Tests.Inventory;

public sealed class InventoryServiceTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 10, 30, 0);

    private readonly FakeStockRepository _stock = new();
    private readonly FakeCatalogRepository _catalog = new();
    private readonly FakeRecipeRepository _recipes = new();
    private readonly FakeBatchRepository _batches = new();

    /// <summary>Солод — крок 0,5 кг, хміль — 50 г, дріжджі — 1 пак. Кількість = більша з нестач до мінімуму й до потреби; null — докуповувати нічого.</summary>
    public static TheoryData<Ingredient, double, double, double, double?> OrderCases => new()
    {
        { Pilsner(), 3.0, 5.2, 0.0, 2.5 },
        { Pilsner(), 3.0, 5.0, 9.1, 6.5 },
        { BoilHop(), 120.0, 200.0, 160.0, 100.0 },
        { BoilHop(), 100.0, 120.0, 220.0, 150.0 },
        { BoilHop(), 100.0, 200.0, 0.0, 100.0 },
        { Us05(), 1.0, 3.0, 5.2, 5.0 },
        { Us05(), 1.0, 3.0, 0.0, 2.0 },
        { Pilsner(), 10.0, 5.0, 8.0, null },
    };

    /// <summary>Вручну: нестача округлюється вгору до кроку; сума = кількість · 40 грн.</summary>
    [Theory]
    [MemberData(nameof(OrderCases))]
    public void BuildPurchaseOrder_BelowMinimumOrPlannedNeed_RoundsUpLargerShortage(Ingredient ingredient, double available, double minStock, double plannedNeed, double? expected)
    {
        var item = AddToCatalog(ingredient);
        _catalog.MinStocks[item.CatalogId] = minStock;
        AddLot(item, left: available);
        if (plannedNeed > 0)
        {
            item.Amount = plannedNeed;
            _batches.Insert(new Batch { RecipeId = _recipes.Insert(RecipeOf(10, item)), VolumeL = 10 });
        }

        var order = ServiceWith(Permission.ManageStock).BuildPurchaseOrder(null);

        if (expected is { } quantity)
        {
            Assert.Equal(quantity, Assert.Single(order.Lines).Quantity, 0.0001);
            Assert.Equal((decimal)quantity * 40m, order.Total);
        }
        else
        {
            Assert.Empty(order.Lines);
        }
    }

    /// <summary>Вручну: k = 100 / 50 = 2; Citra (60 + 100) · 2 = 320 г; солод 12,2 · 2 = 24,4 кг.</summary>
    [Fact]
    public void Requirement_DoubleVolume_ScalesAndMergesLinesOfOneItem()
    {
        var malt = Pilsner();
        malt.Amount = 12.2;
        var boil = DryHop();
        boil.Amount = 60;
        boil.Use = HopUse.Boil;
        var dry = DryHop();
        dry.Amount = 100;

        var need = WarehouseInventory.Requirement(RecipeOf(50, malt, boil, dry), 100);

        Assert.Equal(2, need.Count);
        Assert.Equal(24.4, need[malt.CatalogId], 0.0001);
        Assert.Equal(320, need[boil.CatalogId], 0.0001);
    }

    [Fact]
    public void CheckAvailability_NotEnoughHop_ReturnsShortageWithNameAndAmounts()
    {
        var malt = AddToCatalog(Pilsner());
        var hop = AddToCatalog(DryHop());
        AddLot(malt, left: 20);
        AddLot(hop, left: 120);
        malt.Amount = 10;
        hop.Amount = 160;

        var result = ServiceWith(Permission.ManageBatches).CheckAvailability(RecipeOf(50, malt, hop), 50);

        Assert.False(result.IsSufficient);
        var shortage = Assert.Single(result.Shortages);
        Assert.Equal("Citra", shortage.Name);
        Assert.Equal("г", shortage.Unit);
        Assert.Equal(160, shortage.Required, 0.0001);
        Assert.Equal(120, shortage.Available, 0.0001);
    }

    /// <summary>Вручну: 3 кг із партії за 40 грн і 3 кг із партії за 50 грн = 120 + 150 = 270 грн.</summary>
    [Fact]
    public void WriteOffForBatch_TwoLots_AppliesPartsAndReturnsTheirCost()
    {
        var malt = AddToCatalog(Pilsner());
        AddLot(malt, left: 3, price: 40m, expiresInDays: 10);
        AddLot(malt, left: 5, price: 50m, expiresInDays: 20);
        malt.Amount = 6;

        var cost = ServiceWith(Permission.ManageBatches).WriteOffForBatch(7, RecipeOf(20, malt), 20, userId: 3);

        Assert.Equal(270m, cost);
        Assert.Equal(2, _stock.AppliedParts.Count);
        Assert.Equal(7, _stock.AppliedBatchId);
        Assert.Equal(3, _stock.AppliedUserId);
    }

    [Fact]
    public void ReceiveStock_NoManageStock_ThrowsAndStoresNothing()
    {
        var malt = AddToCatalog(Pilsner());

        Assert.Throws<DomainException>(() =>
            ServiceWith(Permission.ViewStock).ReceiveStock(malt.CatalogId, 10, 40m, null, null, null));

        Assert.Empty(_stock.GetAll());
    }

    /// <summary>Вручну: (10·40 + 30·60) / 40 = 55 грн; без залишків — остання ціна 42 грн.</summary>
    [Theory]
    [InlineData(true, 55.0)]
    [InlineData(false, 42.0)]
    public void CurrentUnitPrice_WithOrWithoutLots_ReturnsWeightedOrLastPrice(bool hasLots, double expected)
    {
        var malt = AddToCatalog(Pilsner());
        _catalog.LastPrices[malt.CatalogId] = 42m;
        if (hasLots)
        {
            AddLot(malt, left: 10, price: 40m);
            AddLot(malt, left: 30, price: 60m);
        }

        Assert.Equal((decimal)expected, ServiceWith(Permission.ViewStock).CurrentUnitPrice(malt.CatalogId));
    }

    private T AddToCatalog<T>(T item) where T : Ingredient
    {
        _catalog.Insert(item);
        item.CatalogId = item.Id;
        return item;
    }

    private void AddLot(Ingredient item, double left, decimal price = 40m, int expiresInDays = 30) => _stock.Insert(new StockLot
    {
        CatalogId = item.CatalogId,
        QuantityReceived = left,
        QuantityLeft = left,
        UnitPrice = price,
        ReceivedAt = Now.AddDays(-5),
        ExpiresAt = Now.Date.AddDays(expiresInDays),
    });

    private InventoryService ServiceWith(params Permission[] permissions) =>
        new(_stock, _catalog, _recipes, _batches, new FakeSession(null, permissions), new FixedClock(Now));
}
