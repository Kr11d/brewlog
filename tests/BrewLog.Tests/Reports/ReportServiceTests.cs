using BrewLog.Domain.Batches;
using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.Tests.Fakes;

namespace BrewLog.Tests.Reports;

public sealed class ReportServiceTests
{
    private readonly FakeBatchRepository _batches = new();

    public static TheoryData<Action<ReportService>> ReportCalls => new()
    {
        service => service.GetMonthSummary(2026, 3),
        service => service.GetVolumeByMonth(2026, 3),
        service => service.GetCostByRecipe(),
        service => service.GetAbvComparison(),
    };

    /// <summary>
    /// Вручну: у березні зварено партії B і C (2), розлито A і B: 50 + 40 = 90 л; собівартість (40 + 50) / 2 = 45 грн/л;
    /// ABV: A = (1,050 − 1,0125) · 131,25 = 4,921875 при плані 4,7; B = (1,0525 − 1,0125) · 131,25 = 5,25 при плані 5,5;
    /// відхилення (0,221875 − 0,25) / 2 = −0,0140625.
    /// </summary>
    [Fact]
    public void GetMonthSummary_MixedBatches_CountsOnlySelectedMonth()
    {
        AddPackaged(recipeId: 1, brewedAt: new DateTime(2026, 2, 20), packagedAt: new DateTime(2026, 3, 5), bottles: 100, cost: 2000m, og: 1.050, plannedAbv: 4.7);
        AddPackaged(recipeId: 2, brewedAt: new DateTime(2026, 3, 3), packagedAt: new DateTime(2026, 3, 28), bottles: 80, cost: 2000m, og: 1.0525, plannedAbv: 5.5);
        _batches.Insert(new Batch { RecipeId = 3, State = new FermentingState(), BrewedAt = new DateTime(2026, 3, 20), IngredientsCost = 500m });
        AddPackaged(recipeId: 1, brewedAt: new DateTime(2026, 1, 5), packagedAt: new DateTime(2026, 2, 10), bottles: 100, cost: 9000m, og: 1.070, plannedAbv: 4.7);

        var summary = Service().GetMonthSummary(2026, 3);

        Assert.Equal(2, summary.BrewedCount);
        Assert.Equal(90, summary.PackagedLiters, 0.0001);
        Assert.Equal(45m, summary.AverageCostPerLiter);
        Assert.Equal(-0.0140625, summary.AverageAbvDeviation!.Value, 0.0001);
    }

    /// <summary>Вручну: рецепт 1 має партії по 40 і 50 грн/л, партія в бродінні й рецепт 2 без розливу не входять.</summary>
    [Fact]
    public void GetCostByRecipe_BatchesWithoutPackaging_AreExcluded()
    {
        AddPackaged(recipeId: 1, brewedAt: new DateTime(2026, 2, 20), packagedAt: new DateTime(2026, 3, 5), bottles: 100, cost: 2000m, og: 1.050, plannedAbv: 4.7);
        AddPackaged(recipeId: 1, brewedAt: new DateTime(2026, 3, 3), packagedAt: new DateTime(2026, 3, 28), bottles: 80, cost: 2000m, og: 1.050, plannedAbv: 4.7);
        _batches.Insert(new Batch { RecipeId = 1, RecipeName = "Рецепт 1", State = new FermentingState(), IngredientsCost = 9000m });
        _batches.Insert(new Batch { RecipeId = 2, RecipeName = "Рецепт 2", State = new ConditioningState(), IngredientsCost = 9000m });

        var row = Assert.Single(Service().GetCostByRecipe());

        Assert.Equal("Рецепт 1", row.RecipeName);
        Assert.Equal(2, row.BatchCount);
        Assert.Equal(45m, row.AverageCostPerLiter);
        Assert.Equal(40m, row.MinCostPerLiter);
        Assert.Equal(50m, row.MaxCostPerLiter);
    }

    /// <summary>12 місяців, що закінчуються березнем 2026, починаються квітнем 2025; розливи лише в лютому й березні.</summary>
    [Fact]
    public void GetVolumeByMonth_FewPackagings_ReturnsTwelveMonthsIncludingEmpty()
    {
        AddPackaged(recipeId: 1, brewedAt: new DateTime(2026, 2, 1), packagedAt: new DateTime(2026, 3, 5), bottles: 100, cost: 2000m, og: 1.050, plannedAbv: 4.7);
        AddPackaged(recipeId: 1, brewedAt: new DateTime(2026, 1, 1), packagedAt: new DateTime(2026, 2, 5), bottles: 80, cost: 2000m, og: 1.050, plannedAbv: 4.7);

        var volumes = Service().GetVolumeByMonth(2026, 3);

        Assert.Equal(12, volumes.Count);
        Assert.Equal((2025, 4), (volumes[0].Year, volumes[0].Month));
        Assert.Equal((2026, 3), (volumes[11].Year, volumes[11].Month));
        Assert.Equal(50, volumes[11].Liters, 0.0001);
        Assert.Equal(40, volumes[10].Liters, 0.0001);
        Assert.All(volumes.Take(10), volume => Assert.Equal(0, volume.Liters, 0.0001));
    }

    [Theory]
    [MemberData(nameof(ReportCalls))]
    public void AnyReport_NoViewReports_Throws(Action<ReportService> call)
    {
        var service = new ReportService(_batches, new FakeSession(null, Permission.ViewBatches));

        Assert.Throws<DomainException>(() => call(service));
    }

    private ReportService Service() => new(_batches, new FakeSession(null, Permission.ViewReports));

    private void AddPackaged(int recipeId, DateTime brewedAt, DateTime packagedAt, int bottles, decimal cost, double og, double plannedAbv) =>
        _batches.Insert(new Batch
        {
            RecipeId = recipeId,
            RecipeName = $"Рецепт {recipeId}",
            VolumeL = 50,
            State = new PackagedState(),
            BrewedAt = brewedAt,
            PackagedAt = packagedAt,
            ActualOg = og,
            PlannedAbv = plannedAbv,
            IngredientsCost = cost,
            Packaging = new Packaging { BottlesCount = bottles, BottleVolumeL = 0.5, FinalGravity = 1.0125 },
        });
}
