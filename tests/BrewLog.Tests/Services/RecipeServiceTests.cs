using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.Tests.Fakes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Services;

public sealed class RecipeServiceTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 10, 30, 0);

    private readonly FakeRecipeRepository _recipes = new();
    private readonly FakeCatalogRepository _catalog = new();

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
        Assert.Equal(5, original.Ingredients[0].Amount);
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

        var cost = ServiceWith().EstimateCostPerLiter(RecipeOf(20, malt, hop, yeastWithoutPrice));

        Assert.Equal(13m, cost);
    }

    private RecipeService ServiceWith(params Permission[] permissions) =>
        new(_recipes, _catalog, new FakeSession(null, permissions), new FixedClock(Now));
}
