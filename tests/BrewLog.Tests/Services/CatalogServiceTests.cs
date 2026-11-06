using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.Tests.Fakes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Services;

public sealed class CatalogServiceTests
{
    private readonly FakeStyleRepository _styles = new();
    private readonly FakeCatalogRepository _catalog = new();

    public static TheoryData<Action<CatalogService>> WriteOperations => new()
    {
        service => service.SaveStyle(new BeerStyle { Code = "21A", Name = "American IPA" }),
        service => service.DeleteStyle(1),
        service => service.SaveCatalogItem(BoilHop()),
        service => service.DeleteCatalogItem(1),
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Delete_UsedStyleOrIngredient_ThrowsAndKeepsIt(bool isStyle)
    {
        var service = ServiceWith(Permission.EditReferences);
        var styleId = _styles.Insert(new BeerStyle { Code = "21A", Name = "American IPA" });
        var itemId = _catalog.Insert(BoilHop());
        _styles.UsedIds.Add(styleId);
        _catalog.UsedIds.Add(itemId);

        Assert.Throws<DomainException>(() =>
        {
            if (isStyle)
            {
                service.DeleteStyle(styleId);
            }
            else
            {
                service.DeleteCatalogItem(itemId);
            }
        });

        Assert.NotNull(_styles.GetById(styleId));
        Assert.NotNull(_catalog.GetById(itemId));
    }

    [Theory]
    [MemberData(nameof(WriteOperations))]
    public void WriteOperation_NoEditReferences_Throws(Action<CatalogService> write)
    {
        var service = ServiceWith(Permission.ViewRecipes, Permission.EditRecipes);

        Assert.Throws<DomainException>(() => write(service));
    }

    [Fact]
    public void SaveCatalogItem_ItemWithoutAmount_IsStored()
    {
        var item = BoilHop();
        item.Amount = 0;

        var id = ServiceWith(Permission.EditReferences).SaveCatalogItem(item);

        Assert.NotNull(_catalog.GetById(id));
    }

    private CatalogService ServiceWith(params Permission[] permissions) =>
        new(_styles, _catalog, new FakeSession(null, permissions));
}
