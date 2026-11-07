using BrewLog.Domain.Recipes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Recipes;

public sealed class IngredientTests
{
    public static TheoryData<Ingredient> ValidIngredients => new()
    {
        Pilsner(),
        Sucrose(),
        BoilHop(),
        DryHop(),
        Us05(),
    };

    public static TheoryData<Ingredient> OutOfRangeIngredients
    {
        get
        {
            var malts = new Action<Malt>[]
            {
                malt => malt.Amount = 0,
                malt => malt.YieldPercent = -1,
                malt => malt.YieldPercent = 101,
                malt => malt.ColorLovibond = -1,
            };
            var hops = new Action<Hop>[]
            {
                hop => hop.Amount = -5,
                hop => hop.AlphaPercent = -1,
                hop => hop.AlphaPercent = 26,
                hop => hop.TimeMin = -1,
            };
            var yeasts = new Action<Yeast>[]
            {
                yeast => yeast.Amount = 0,
                yeast => yeast.AttenuationPercent = 49,
                yeast => yeast.AttenuationPercent = 101,
                yeast => yeast.TempMinC = yeast.TempMaxC,
            };
            var adjuncts = new Action<Adjunct>[]
            {
                adjunct => adjunct.Amount = 0,
                adjunct => adjunct.YieldPercent = 101,
            };

            var data = new TheoryData<Ingredient>();
            AddBroken(data, Pilsner, malts);
            AddBroken(data, BoilHop, hops);
            AddBroken(data, () => Us05(), yeasts);
            AddBroken(data, () => Sucrose(), adjuncts);
            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ValidIngredients))]
    public void Validate_ValidIngredient_ReturnsNoErrors(Ingredient ingredient)
    {
        Assert.Empty(ingredient.Validate());
    }

    [Theory]
    [MemberData(nameof(OutOfRangeIngredients))]
    public void Validate_ValueOutOfRange_ReturnsError(Ingredient ingredient)
    {
        Assert.NotEmpty(ingredient.Validate());
    }

    [Theory]
    [InlineData(IngredientKind.Malt, typeof(Malt))]
    [InlineData(IngredientKind.Hop, typeof(Hop))]
    [InlineData(IngredientKind.Yeast, typeof(Yeast))]
    [InlineData(IngredientKind.Adjunct, typeof(Adjunct))]
    public void FromData_Kind_CreatesMatchingClass(IngredientKind kind, Type expectedType)
    {
        var ingredient = IngredientFactory.FromData(new IngredientData(kind));

        Assert.IsType(expectedType, ingredient);
        Assert.Equal(kind, ingredient.Kind);
    }

    [Theory]
    [MemberData(nameof(ValidIngredients))]
    public void ToDataFromData_AnyIngredient_PreservesFields(Ingredient ingredient)
    {
        ingredient.Id = 42;
        var data = ingredient.ToData();

        var restored = IngredientFactory.FromData(data);

        Assert.Equal(data, restored.ToData());
        Assert.Equal(ingredient.GetType(), restored.GetType());
    }

    [Fact]
    public void Clone_ChangedCopy_LeavesOriginalIntact()
    {
        var original = Pilsner();

        var copy = Assert.IsType<Malt>(original.Clone());
        copy.Amount = 99;
        copy.YieldPercent = 1;

        Assert.Equal(5, original.Amount, 0.0001);
        Assert.Equal(80, original.YieldPercent, 0.0001);
    }

    [Fact]
    public void DescribeContribution_EachKind_FormatsOwnContribution()
    {
        var malt = Pilsner();
        var hop = BoilHop();
        var yeast = Us05();
        var sucrose = Sucrose();

        var pilsnerStats = RecipeCalculator.Calculate(RecipeOf(23, malt, hop, yeast));
        var sucroseStats = RecipeCalculator.Calculate(RecipeOf(23, sucrose));

        Assert.Equal("+0,048 OG · 3,4 SRM", malt.DescribeContribution(pilsnerStats));
        Assert.Equal("30,6 IBU", hop.DescribeContribution(pilsnerStats));
        Assert.Equal("зброджування 77 %", yeast.DescribeContribution(pilsnerStats));
        Assert.Equal("+0,008 OG", sucrose.DescribeContribution(sucroseStats));
    }

    private static void AddBroken<T>(TheoryData<Ingredient> data, Func<T> create, IEnumerable<Action<T>> breakers)
        where T : Ingredient
    {
        foreach (var breakIngredient in breakers)
        {
            var ingredient = create();
            breakIngredient(ingredient);
            data.Add(ingredient);
        }
    }
}
