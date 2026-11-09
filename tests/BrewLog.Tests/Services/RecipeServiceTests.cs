using BrewLog.Data.BeerXml;
using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.Tests.Fakes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Services;

public sealed class RecipeServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 3, 15, 10, 30, 0);

    private readonly FakeRecipeRepository _recipes = new();
    private readonly FakeCatalogRepository _catalog = new();
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(), $"brewlog-test-{Guid.NewGuid():N}.xml");

    [Theory]
    [InlineData(false, 23.0)]
    [InlineData(true, 0.0)]
    public void Save_NoPermissionOrInvalidRecipe_ThrowsAndStoresNothing(bool canEdit, double volumeL)
    {
        var service = canEdit ? ServiceWith(Permission.EditRecipes) : ServiceWith(Permission.ViewRecipes);

        Assert.Throws<DomainException>(() => service.Save(RecipeOf(volumeL, Pilsner())));

        Assert.Empty(_recipes.GetAll());
    }

    [Fact]
    public void Save_ChangedRecipe_UpdatesTimeButKeepsCreationTime()
    {
        var created = new DateTime(2026, 1, 1);
        var recipe = RecipeOf(23, Pilsner());
        recipe.CreatedAt = created;
        _recipes.Insert(recipe);

        ServiceWith(Permission.EditRecipes).Save(recipe);

        Assert.Equal(Now, recipe.UpdatedAt);
        Assert.Equal(created, recipe.CreatedAt);
    }

    [Fact]
    public void GetAll_AssistantPermissions_Throws()
    {
        var service = ServiceWith(Permission.ViewBatches, Permission.AddMeasurements, Permission.PackageBatches);

        Assert.Throws<DomainException>(() => service.GetAll());
    }

    [Fact]
    public void Duplicate_ExistingRecipe_SavesCopyWithNewNameAndIndependentLines()
    {
        var original = RecipeOf(23, Pilsner());
        original.Name = "American IPA";
        _recipes.Insert(original);

        var copy = ServiceWith(Permission.EditRecipes).Duplicate(original.Id);
        copy.Ingredients[0].Amount = 99;

        Assert.Equal("American IPA (копія)", copy.Name);
        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal(5, original.Ingredients[0].Amount, 0.0001);
        Assert.Equal(2, _recipes.GetAll().Count);
    }

    [Fact]
    public void Delete_RecipeUsedInBatches_ThrowsAndKeepsRecipe()
    {
        var id = _recipes.Insert(RecipeOf(23, Pilsner()));
        _recipes.UsedInBatches.Add(id);

        var error = Assert.Throws<DomainException>(() => ServiceWith(Permission.EditRecipes).Delete(id));

        Assert.Equal("Рецепт використовується в партіях", error.Message);
        Assert.NotNull(_recipes.GetById(id));
    }

    /// <summary>Вручну: (5 кг · 40 грн + 30 г · 2 грн) / 20 л = 260 / 20 = 13 грн/л; рядок без ціни нічого не додає.</summary>
    [Fact]
    public void EstimateCostPerLiter_KnownPrices_SumsAmountTimesPricePerLiter()
    {
        var malt = Pilsner();
        var hop = BoilHop();
        var yeastWithoutPrice = Us05();
        _catalog.LastPrices[malt.CatalogId] = 40m;
        _catalog.LastPrices[hop.CatalogId] = 2m;

        var cost = ServiceWith(Permission.ViewStock).EstimateCostPerLiter(RecipeOf(20, malt, hop, yeastWithoutPrice));

        Assert.Equal(13m, cost);
    }

    [Fact]
    public void Import_MissingAndExistingIngredients_AddsOnlyMissingToCatalog()
    {
        var pilsner = Pilsner();
        pilsner.Amount = 0;
        _catalog.Insert(pilsner);
        File.WriteAllText(_tempFile, """
            <RECIPES>
              <RECIPE>
                <NAME>Перший</NAME><BATCH_SIZE>20</BATCH_SIZE>
                <FERMENTABLES>
                  <FERMENTABLE><NAME>PILSNER</NAME><TYPE>Grain</TYPE><AMOUNT>5</AMOUNT><YIELD>80</YIELD><COLOR>2</COLOR></FERMENTABLE>
                </FERMENTABLES>
                <HOPS>
                  <HOP><NAME>Nelson</NAME><AMOUNT>0.03</AMOUNT><ALPHA>12</ALPHA><TIME>60</TIME><USE>Boil</USE></HOP>
                </HOPS>
              </RECIPE>
              <RECIPE>
                <NAME>Другий</NAME><BATCH_SIZE>20</BATCH_SIZE>
                <HOPS>
                  <HOP><NAME>nelson</NAME><AMOUNT>0.01</AMOUNT><ALPHA>11</ALPHA><TIME>5</TIME><USE>Boil</USE></HOP>
                </HOPS>
              </RECIPE>
            </RECIPES>
            """);

        var result = ServiceWith(Permission.EditRecipes).Import(_tempFile);

        Assert.Equal(1, result.NewCatalogItems);
        Assert.Equal(2, result.RecipeIds.Count);
        var nelson = _catalog.FindByName(IngredientKind.Hop, "Nelson")!;
        Assert.Equal(0, nelson.Amount, 0.0001);
        Assert.Equal(2, _catalog.GetAll().Count);
        var first = _recipes.GetById(result.RecipeIds[0])!;
        Assert.Equal(pilsner.Id, first.IngredientsOf<Malt>()[0].CatalogId);
        Assert.Equal("Pilsner", first.IngredientsOf<Malt>()[0].Name);
        Assert.Equal(nelson.Id, first.IngredientsOf<Hop>()[0].CatalogId);
        Assert.Equal(nelson.Id, _recipes.GetById(result.RecipeIds[1])!.IngredientsOf<Hop>()[0].CatalogId);
    }

    /// <summary>Другий рецепт файлу некоректний: перший теж не зберігається, а нова позиція не потрапляє в довідник.</summary>
    [Theory]
    [InlineData(false, 20.0)]
    [InlineData(true, 0.0)]
    public void Import_NoPermissionOrInvalidRecipe_ThrowsAndStoresNothing(bool canEdit, double secondVolumeL)
    {
        var service = canEdit ? ServiceWith(Permission.EditRecipes) : ServiceWith(Permission.ViewRecipes);
        File.WriteAllText(_tempFile, $"""
            <RECIPES>
              <RECIPE>
                <NAME>Перший</NAME><BATCH_SIZE>20</BATCH_SIZE>
                <HOPS>
                  <HOP><NAME>Nelson</NAME><AMOUNT>0.03</AMOUNT><ALPHA>12</ALPHA><TIME>60</TIME><USE>Boil</USE></HOP>
                </HOPS>
              </RECIPE>
              <RECIPE><NAME>Другий</NAME><BATCH_SIZE>{secondVolumeL}</BATCH_SIZE></RECIPE>
            </RECIPES>
            """);

        Assert.Throws<DomainException>(() => service.Import(_tempFile));

        Assert.Empty(_recipes.GetAll());
        Assert.Empty(_catalog.GetAll());
    }

    [Fact]
    public void Export_ExistingRecipe_WritesFileThatImportsBack()
    {
        var recipe = RecipeOf(23, Pilsner());
        _recipes.Insert(recipe);
        var service = ServiceWith(Permission.EditRecipes);

        service.Export([recipe.Id], _tempFile);
        var result = service.Import(_tempFile);

        var imported = _recipes.GetById(result.RecipeIds[0])!;
        Assert.NotEqual(recipe.Id, imported.Id);
        Assert.Equal(recipe.Name, imported.Name);
        Assert.Equal(5, imported.IngredientsOf<Malt>()[0].Amount, 0.0001);
    }

    public void Dispose() => File.Delete(_tempFile);

    private RecipeService ServiceWith(params Permission[] permissions)
    {
        var session = new FakeSession(null, permissions);
        var clock = new FixedClock(Now);
        var inventory = new InventoryService(new FakeStockRepository(), _catalog, _recipes, new FakeBatchRepository(), session, clock);
        return new RecipeService(_recipes, _catalog, new BeerXmlSerializer(new FakeStyleRepository()), session, clock, inventory);
    }
}
