using BrewLog.Data.BeerXml;
using BrewLog.Domain.Batches;
using BrewLog.Domain.Common;
using BrewLog.Domain.Inventory;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.Tests.Fakes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Services;

public sealed class BatchServiceTests
{
    private const int MyId = 5;
    private const int OtherAssistantId = 6;

    private static readonly DateTime Now = new(2026, 3, 15, 10, 30, 0);

    private readonly FakeBatchRepository _batches = new();
    private readonly FakeRecipeRepository _recipes = new();
    private readonly FakeStyleRepository _styles = new();
    private readonly FakeCatalogRepository _catalog = new();
    private readonly FakeStockRepository _stock = new();

    public static TheoryData<Action<BatchService>> ForbiddenActions => new()
    {
        service => service.GetAll(),
        service => service.GetAlerts(1),
        service => service.Plan(1, 20, Now, null),
        service => service.CanAdvance(1),
        service => service.Advance(1),
        service => service.SetActualOg(1, 1.050),
        service => service.AddMeasurement(1, Now, 1.020, null, null),
        service => service.Package(1, new Packaging { BottlesCount = 10 }),
    };

    [Theory]
    [MemberData(nameof(ForbiddenActions))]
    public void AnyAction_NoPermission_Throws(Action<BatchService> action)
    {
        _batches.Insert(new Batch());

        Assert.Throws<DomainException>(() => action(ServiceFor(null)));
    }

    [Fact]
    public void Advance_NotEnoughStock_ThrowsAndKeepsPlannedState()
    {
        var batch = PlannedBatch(stockLeft: 1);

        var error = Assert.Throws<DomainException>(() => ServiceFor(null, Permission.ManageBatches).Advance(batch.Id));

        Assert.Contains("Pilsner", error.Message);
        Assert.Equal("Planned", batch.State.Code);
        Assert.Null(batch.IngredientsCost);
        Assert.Equal(0, _batches.StateUpdates);
    }

    [Fact]
    public void Advance_WriteOffFails_KeepsStateAndWritesNoCost()
    {
        var batch = PlannedBatch(stockLeft: 10);
        _stock.WriteOffFailure = new DomainException("Збій списання.");

        Assert.Throws<DomainException>(() => ServiceFor(null, Permission.ManageBatches).Advance(batch.Id));

        Assert.Equal("Planned", batch.State.Code);
        Assert.Null(batch.BrewedAt);
        Assert.Null(batch.IngredientsCost);
        Assert.Equal(0, _batches.StateUpdates);
    }

    /// <summary>Вручну: партія вдвічі більша за рецепт, 5 кг · 2 = 10 кг по 40 грн = 400 грн.</summary>
    [Fact]
    public void Advance_EnoughStock_MovesToBrewingAndRecordsCost()
    {
        var batch = PlannedBatch(stockLeft: 15);

        ServiceFor(null, Permission.ManageBatches).Advance(batch.Id);

        Assert.Equal("Brewing", batch.State.Code);
        Assert.Equal(Now, batch.BrewedAt);
        Assert.Equal(400m, batch.IngredientsCost);
        Assert.Equal(batch.Id, _stock.AppliedBatchId);
        Assert.Equal(1, _batches.StateUpdates);
    }

    [Theory]
    [InlineData(MyId, true)]
    [InlineData(null, true)]
    [InlineData(OtherAssistantId, false)]
    public void AddMeasurement_Assistant_AllowedForOwnOrUnassignedOnly(int? batchAssistantId, bool allowed)
    {
        var batch = new Batch { State = new FermentingState(), AssistantId = batchAssistantId };
        _batches.Insert(batch);
        var service = ServiceFor(new Assistant { Id = MyId }, Permission.AddMeasurements);

        if (allowed)
        {
            var measurement = service.AddMeasurement(batch.Id, Now, 1.020, 20, null);
            Assert.Equal(MyId, measurement.UserId);
            Assert.Single(batch.Measurements);
        }
        else
        {
            Assert.Throws<DomainException>(() => service.AddMeasurement(batch.Id, Now, 1.020, 20, null));
            Assert.Empty(batch.Measurements);
        }
    }

    /// <summary>Еталон HANDOFF §6.1: OG 1,050 і зброджування 75 % дають FG 1,0125 і ABV 4,92; масштаб густини не змінює.</summary>
    [Fact]
    public void Plan_Recipe_StoresRecipeSnapshot()
    {
        var styleId = _styles.Insert(new BeerStyle { Name = "American IPA" });
        var recipe = OgFiftyRecipe(Us05(attenuationPercent: 75));
        recipe.Name = "Тестовий ель";
        recipe.StyleId = styleId;
        _recipes.Insert(recipe);

        var batch = ServiceFor(new Brewer { Id = 2 }, Permission.ManageBatches).Plan(recipe.Id, 15.424, Now.AddDays(3), MyId);

        Assert.Equal("BL-2026-001", batch.Number);
        Assert.Equal("Planned", batch.State.Code);
        Assert.Equal("Тестовий ель", batch.RecipeName);
        Assert.Equal("American IPA", batch.StyleName);
        Assert.Equal(1.050, batch.PlannedOg, 0.0001);
        Assert.Equal(1.0125, batch.PlannedFg, 0.0001);
        Assert.Equal(4.92, batch.PlannedAbv, 0.01);
        Assert.Equal(15, batch.TempMinC!.Value, 0.0001);
        Assert.Equal(24, batch.TempMaxC!.Value, 0.0001);
        Assert.Equal(MyId, batch.AssistantId);
        Assert.Equal(2, batch.CreatedBy);
        Assert.Same(batch, _batches.GetById(batch.Id));
    }

    /// <summary>Рецепт на 5 кг солоду й 10 л; партія на 20 л потребує 10 кг.</summary>
    private Batch PlannedBatch(double stockLeft)
    {
        var malt = Pilsner();
        _catalog.Insert(malt);
        malt.CatalogId = malt.Id;
        _stock.Insert(new StockLot
        {
            CatalogId = malt.CatalogId,
            QuantityReceived = stockLeft,
            QuantityLeft = stockLeft,
            UnitPrice = 40m,
            ReceivedAt = Now.AddDays(-5),
            ExpiresAt = Now.AddDays(30),
        });

        var recipeId = _recipes.Insert(RecipeOf(10, malt));
        var batch = new Batch { RecipeId = recipeId, VolumeL = 20 };
        _batches.Insert(batch);
        return batch;
    }

    private BatchService ServiceFor(User? user, params Permission[] permissions)
    {
        var session = new FakeSession(user, permissions);
        var clock = new FixedClock(Now);
        var inventory = new InventoryService(_stock, _catalog, _recipes, session, clock);
        var recipeService = new RecipeService(_recipes, _catalog, new BeerXmlSerializer(_styles), session, clock, inventory);
        return new BatchService(_batches, _recipes, _styles, recipeService, inventory, session, clock);
    }
}
