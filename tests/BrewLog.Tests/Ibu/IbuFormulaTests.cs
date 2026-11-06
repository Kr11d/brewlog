using BrewLog.Domain.Common;
using BrewLog.Domain.Ibu;
using BrewLog.Domain.Recipes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.Ibu;

public sealed class IbuFormulaTests
{
    private const double IbuTolerance = 0.2;

    [Theory]
    [InlineData(60.0, 30.1)]
    [InlineData(0.0, 0.0)]
    public void Tinseth_BoilTime_GivesReferenceIbu(double timeMin, double expectedIbu)
    {
        var hop = BoilHop();
        hop.TimeMin = timeMin;

        var ibu = new TinsethFormula().Calculate(hop, boilGravity: 1.050, volumeL: 23);

        Assert.Equal(expectedIbu, ibu, IbuTolerance);
    }

    [Theory]
    [InlineData(1.050, 40.2)]
    [InlineData(1.070, 36.5)]
    public void Rager_BoilGravity_GivesReferenceIbu(double boilGravity, double expectedIbu)
    {
        var ibu = new RagerFormula().Calculate(BoilHop(), boilGravity, volumeL: 23);

        Assert.Equal(expectedIbu, ibu, IbuTolerance);
    }

    /// <summary>Кількість сахарози на 23 л підібрана під OG 1,050 і 1,070: формула має брати G = OG рецепта.</summary>
    [Theory]
    [InlineData("Tinseth", 2.9824, 30.1)]
    [InlineData("Rager", 2.9824, 40.2)]
    [InlineData("Rager", 4.1753, 36.5)]
    public void Calculate_BoilHop_UsesRecipeFormulaAndOgAsBoilGravity(string code, double sucroseKilograms, double expectedIbu)
    {
        var recipe = RecipeOf(23, Sucrose(sucroseKilograms), BoilHop());
        recipe.IbuFormulaCode = code;

        var stats = RecipeCalculator.Calculate(recipe);

        Assert.Equal(expectedIbu, stats.Ibu, IbuTolerance);
    }

    [Theory]
    [InlineData("Tinseth")]
    [InlineData("Rager")]
    public void Calculate_DryHop_AddsNoBitterness(string code)
    {
        var hop = DryHop();
        var recipe = RecipeOf(23, Pilsner(), hop);
        recipe.IbuFormulaCode = code;

        var stats = RecipeCalculator.Calculate(recipe);

        Assert.Equal(0, stats.Ibu);
        Assert.Equal(0, stats.IbuOf(hop));
    }

    [Fact]
    public void FromCode_UnknownCode_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => IbuFormulaFactory.FromCode("Garetz"));
    }
}
