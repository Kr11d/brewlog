using BrewLog.Data.Repositories;
using BrewLog.Data.Seed;
using BrewLog.Domain.Batches;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.Tests.Fakes;

namespace BrewLog.Tests.Data;

public sealed class DemoBatchesTests : IDisposable
{
    private readonly TempDatabase _database = new(DemoSeeders.All(new FakePasswordHasher()));
    private readonly BatchRepository _batches;

    public DemoBatchesTests()
    {
        _batches = new BatchRepository(_database.Connections);
    }

    private DateTime Noon => _database.Today.AddHours(12);

    public void Dispose() => _database.Dispose();

    [Fact]
    public void DemoPlannedIpa_CheckAvailability_LacksCitraAndMosaicOnly()
    {
        var recipes = new RecipeRepository(_database.Connections);
        var inventory = new InventoryService(
            new StockRepository(_database.Connections, new FixedClock(Noon)),
            new IngredientCatalogRepository(_database.Connections),
            recipes,
            _batches,
            new FakeSession(null, Permission.ManageBatches),
            new FixedClock(Noon));

        var planned = Assert.Single(_batches.GetPlanned());
        var result = inventory.CheckAvailability(recipes.GetById(planned.RecipeId)!, planned.VolumeL);

        Assert.Equal("American IPA", planned.RecipeName);
        Assert.Equal(50, planned.VolumeL, 0.0001);
        Assert.Equal(_database.Today.AddDays(3), planned.PlannedDate);
        Assert.Equal(["Citra", "Mosaic"], result.Shortages.Select(item => item.Name).Order());
    }

    [Fact]
    public void DemoBatches_ByStage_MatchHandoffCounts()
    {
        var batches = _batches.GetAll();

        Assert.Equal(
            [("Brewing", 0), ("Conditioning", 1), ("Fermenting", 2), ("Packaged", 4), ("Planned", 1)],
            BatchStateFactory.All.OrderBy(state => state.Code, StringComparer.Ordinal).Select(state => (state.Code, batches.Count(batch => batch.State.Code == state.Code))));
        Assert.Equal(batches.Count, batches.Select(batch => batch.Number).Distinct().Count());
        Assert.Contains(batches, batch => batch.AssistantId is not null);
        Assert.All(batches.Where(batch => batch.Packaging is not null), batch =>
        {
            Assert.True(batch.IngredientsCost > 0);
            Assert.True(batch.Packaging!.PackagedVolumeL <= batch.VolumeL);
            Assert.NotNull(batch.ActualAbv);
        });
    }

    [Theory]
    [InlineData(AlertKind.ReadyForConditioning)]
    [InlineData(AlertKind.TemperatureOutOfRange)]
    public void DemoFermentingBatches_Analyzer_IncludeExpectedAlert(AlertKind kind)
    {
        var fermenting = _batches.GetAll().Where(batch => batch.State.TracksFermentation).ToList();

        var withAlert = Assert.Single(fermenting, batch => FermentationAnalyzer.Analyze(batch, Noon).Any(alert => alert.Kind == kind));

        Assert.Single(FermentationAnalyzer.Analyze(withAlert, Noon));
    }
}
