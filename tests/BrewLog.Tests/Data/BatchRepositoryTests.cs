using BrewLog.Data.Repositories;
using BrewLog.Domain.Batches;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Data;

public sealed class BatchRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 3, 15, 10, 30, 45);

    private readonly TempDatabase _database = new();
    private readonly BatchRepository _batches;

    public BatchRepositoryTests()
    {
        _batches = new BatchRepository(_database.Connections);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public void SaveThenGetById_BatchWithMeasurementsAndPackaging_RestoresAll()
    {
        var recipeId = new RecipeRepository(_database.Connections).Insert(RecipeOf(50));
        var batch = new Batch
        {
            Number = "BL-2026-001",
            RecipeId = recipeId,
            RecipeName = "Тестовий ель",
            StyleName = "American IPA",
            VolumeL = 50,
            State = new ConditioningState(),
            PlannedDate = Now.Date,
            FermentingAt = Now.AddDays(-10),
            ConditioningAt = Now.AddDays(-1),
            PlannedOg = 1.050,
            PlannedFg = 1.0125,
            PlannedAbv = 4.92,
            PlannedSrm = 7,
            TempMinC = 15,
            TempMaxC = 24,
            ActualOg = 1.051,
            IngredientsCost = 1250.5m,
        };
        var id = _batches.Insert(batch);
        var later = batch.AddMeasurement(Now, 1.012, 20.5, " Нотатка ", null, Now);
        var earlier = batch.AddMeasurement(Now.AddDays(-1), null, 19, null, null, Now);
        _batches.AddMeasurement(later);
        _batches.AddMeasurement(earlier);

        batch.SetPackaging(new Packaging { PackagedAt = Now, BottlesCount = 56, BottleVolumeL = 0.5, KegsCount = 1, KegVolumeL = 20, FinalGravity = 1.012 });
        batch.Advance(new TransitionContext(batch, Now, null));
        _batches.SavePackaging(batch);

        var loaded = _batches.GetById(id)!;

        Assert.Equal("Packaged", loaded.State.Code);
        Assert.Equal(Now, loaded.PackagedAt);
        Assert.Equal(Now.AddDays(-1), loaded.ConditioningAt);
        Assert.Equal(1250.5m, loaded.IngredientsCost);
        Assert.Equal(1.051, loaded.ActualOg!.Value, 0.0001);
        Assert.Equal("American IPA", loaded.StyleName);

        Assert.Equal(2, loaded.Measurements.Count);
        Assert.Equal(earlier.Id, loaded.Measurements[0].Id);
        Assert.Null(loaded.Measurements[0].Gravity);
        Assert.Equal(1.012, loaded.Measurements[1].Gravity!.Value, 0.0001);
        Assert.Equal("Нотатка", loaded.Measurements[1].Note);

        Assert.Equal(56, loaded.Packaging!.BottlesCount);
        Assert.Equal(1, loaded.Packaging.KegsCount);
        Assert.Equal(2, loaded.Packaging.LossL, 0.0001);
        Assert.Equal(1.012, loaded.Packaging.FinalGravity!.Value, 0.0001);
        Assert.Equal(id, loaded.Packaging.BatchId);
    }
}
