using System.Globalization;
using System.Text;
using System.Xml.Linq;
using BrewLog.Data.BeerXml;
using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;
using BrewLog.Tests.Fakes;
using static BrewLog.Tests.Recipes.SampleIngredients;

namespace BrewLog.Tests.BeerXml;

/// <summary>Усі сценарії йдуть під uk-UA: CI працює під іншою культурою й не показав би десяткову кому у файлі.</summary>
public sealed class BeerXmlTests : IDisposable
{
    private readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;
    private readonly FakeStyleRepository _styles = new();
    private readonly BeerXmlSerializer _serializer;

    public BeerXmlTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("uk-UA");
        _serializer = new BeerXmlSerializer(_styles);
    }

    public void Dispose() => CultureInfo.CurrentCulture = _previousCulture;

    [Fact]
    public void ExportThenImport_AllIngredientTypes_KeepsLinesOgAndFormula()
    {
        var styleId = _styles.Insert(new BeerStyle { Code = "21A", Name = "American IPA", Category = "IPA" });
        var original = RecipeOf(23, Pilsner(), Sucrose(), BoilHop(), DryHop(), Us05());
        original.StyleId = styleId;
        original.IbuFormulaCode = "Rager";
        original.Notes = "Нотатка";

        var imported = Assert.Single(_serializer.Import(ToStream(original)));

        Assert.Single(imported.IngredientsOf<Malt>());
        Assert.Single(imported.IngredientsOf<Adjunct>());
        Assert.Equal(2, imported.IngredientsOf<Hop>().Count);
        Assert.Single(imported.IngredientsOf<Yeast>());
        Assert.Equal(RecipeCalculator.Calculate(original).Og, RecipeCalculator.Calculate(imported).Og, 0.001);
        Assert.Equal(RecipeCalculator.Calculate(original).Ibu, RecipeCalculator.Calculate(imported).Ibu, 0.001);
        Assert.Equal(styleId, imported.StyleId);
        Assert.Equal("Rager", imported.IbuFormulaCode);
        Assert.Equal("Нотатка", imported.Notes);
    }

    [Fact]
    public void Export_UkrainianCulture_WritesDotsVersionsAndEmptySections()
    {
        Assert.Equal("1,5", 1.5.ToString(CultureInfo.CurrentCulture));

        var xml = Encoding.UTF8.GetString(ToStream(RecipeOf(23, Sucrose(kilograms: 0.5), BoilHop(), Us05())).ToArray());
        var recipe = XDocument.Parse(xml).Root!.Element("RECIPE")!;

        Assert.Contains("<AMOUNT>0.5</AMOUNT>", xml);
        Assert.Contains("<AMOUNT>0.03</AMOUNT>", xml);
        Assert.DoesNotContain("0,5", xml);
        string[] records = ["STYLE", "HOPS/HOP", "FERMENTABLES/FERMENTABLE", "YEASTS/YEAST", "MASH"];
        Assert.Equal("1", recipe.Element("VERSION")?.Value);
        Assert.All(records, path => Assert.Equal("1", Find(recipe, path).Element("VERSION")?.Value));
        Assert.Empty(recipe.Element("MISCS")!.Elements());
        Assert.Empty(recipe.Element("WATERS")!.Elements());
    }

    [Fact]
    public void Import_SeveralRecipes_ReturnsEachInFileOrder()
    {
        var recipes = _serializer.Import(ToStream("""
            <RECIPES>
              <RECIPE><NAME>Перший</NAME><BATCH_SIZE>20</BATCH_SIZE></RECIPE>
              <RECIPE><NAME>Другий</NAME><BATCH_SIZE>30.5</BATCH_SIZE></RECIPE>
            </RECIPES>
            """));

        Assert.Equal(["Перший", "Другий"], recipes.Select(recipe => recipe.Name));
        Assert.Equal(30.5, recipes[1].BatchVolumeL, 0.0001);
    }

    [Theory]
    [InlineData("0.07", "Dry Hop", 70.0, HopUse.DryHop)]
    [InlineData("0.0255", "Boil", 25.5, HopUse.Boil)]
    [InlineData("0.03", "Aroma", 30.0, HopUse.Boil)]
    public void Import_Hop_ConvertsKilogramsToGramsAndReadsUse(string kilograms, string use, double expectedGrams, HopUse expectedUse)
    {
        var hop = Assert.Single(ImportLines<Hop>($"""
            <HOPS><HOP><NAME>Citra</NAME><AMOUNT>{kilograms}</AMOUNT><ALPHA>12.5</ALPHA><TIME>15</TIME><USE>{use}</USE></HOP></HOPS>
            """));

        Assert.Equal(expectedGrams, hop.Amount, 0.0001);
        Assert.Equal(expectedUse, hop.Use);
        Assert.Equal(12.5, hop.AlphaPercent, 0.0001);
        Assert.Equal(15, hop.TimeMin, 0.0001);
    }

    [Theory]
    [InlineData("Grain", IngredientKind.Malt, true)]
    [InlineData("Extract", IngredientKind.Malt, false)]
    [InlineData("Dry Extract", IngredientKind.Malt, false)]
    [InlineData("Sugar", IngredientKind.Adjunct, null)]
    [InlineData("Adjunct", IngredientKind.Adjunct, null)]
    public void Import_FermentableType_CreatesRightClassAndMashing(string type, IngredientKind expectedKind, bool? expectedIsMashed)
    {
        var line = Assert.Single(ImportLines<Ingredient>($"""
            <FERMENTABLES>
              <FERMENTABLE><NAME>Позиція</NAME><TYPE>{type}</TYPE><AMOUNT>5</AMOUNT><YIELD>80</YIELD><COLOR>1.8</COLOR></FERMENTABLE>
            </FERMENTABLES>
            """));

        Assert.Equal(expectedKind, line.Kind);
        Assert.Equal(expectedIsMashed, line.ToData().IsMashed);
    }

    [Theory]
    [InlineData("<AMOUNT>3</AMOUNT>", 3.0)]
    [InlineData("<AMOUNT>0.0115</AMOUNT>", 1.0)]
    [InlineData("", 1.0)]
    public void Import_YeastAmount_ReadsPacksOrDefaultsToOne(string amountElement, double expectedPacks)
    {
        var yeast = Assert.Single(ImportLines<Yeast>($"""
            <YEASTS>
              <YEAST><NAME>US-05</NAME>{amountElement}<ATTENUATION>77</ATTENUATION><MIN_TEMPERATURE>15</MIN_TEMPERATURE><MAX_TEMPERATURE>24</MAX_TEMPERATURE></YEAST>
            </YEASTS>
            """));

        Assert.Equal(expectedPacks, yeast.Amount, 0.0001);
        Assert.Equal(77, yeast.AttenuationPercent, 0.0001);
        Assert.Equal(15, yeast.TempMinC, 0.0001);
        Assert.Equal(24, yeast.TempMaxC, 0.0001);
    }

    [Theory]
    [InlineData("21", "A", true)]
    [InlineData("21", "a", true)]
    [InlineData("99", "Z", false)]
    public void Import_StyleCode_FindsStyleOrLeavesWithoutStyle(string number, string letter, bool expectedFound)
    {
        var styleId = _styles.Insert(new BeerStyle { Code = "21A", Name = "American IPA", Category = "IPA" });

        var recipe = Assert.Single(_serializer.Import(ToStream($"""
            <RECIPES>
              <RECIPE>
                <NAME>Рецепт</NAME><BATCH_SIZE>20</BATCH_SIZE>
                <STYLE><CATEGORY_NUMBER>{number}</CATEGORY_NUMBER><STYLE_LETTER>{letter}</STYLE_LETTER></STYLE>
              </RECIPE>
            </RECIPES>
            """)));

        Assert.Equal(expectedFound ? styleId : null, recipe.StyleId);
    }

    [Theory]
    [InlineData("це не XML")]
    [InlineData("<RECIPES><RECIPE>")]
    [InlineData("<OTHER />")]
    [InlineData("<RECIPES />")]
    [InlineData("""<!DOCTYPE RECIPES [<!ENTITY x "y">]><RECIPES />""")]
    [InlineData("<RECIPES><RECIPE><NAME>Без обсягу</NAME></RECIPE></RECIPES>")]
    [InlineData("<RECIPES><RECIPE><BATCH_SIZE>23,5</BATCH_SIZE></RECIPE></RECIPES>")]
    [InlineData("<RECIPES><RECIPE><BATCH_SIZE>1e30</BATCH_SIZE></RECIPE></RECIPES>")]
    [InlineData("<RECIPES><RECIPE><BATCH_SIZE>NaN</BATCH_SIZE></RECIPE></RECIPES>")]
    [InlineData("<RECIPES><RECIPE><BATCH_SIZE>20</BATCH_SIZE><FERMENTABLES><FERMENTABLE><TYPE>Вода</TYPE></FERMENTABLE></FERMENTABLES></RECIPE></RECIPES>")]
    public void Import_InvalidFile_ThrowsDomainException(string content)
    {
        Assert.Throws<DomainException>(() => _serializer.Import(ToStream(content)));
    }

    private IReadOnlyList<T> ImportLines<T>(string lines) where T : Ingredient
    {
        var recipe = Assert.Single(_serializer.Import(ToStream($"<RECIPES><RECIPE><NAME>Рецепт</NAME><BATCH_SIZE>20</BATCH_SIZE>{lines}</RECIPE></RECIPES>")));
        return recipe.IngredientsOf<T>();
    }

    private static XElement Find(XElement parent, string path) =>
        path.Split('/').Aggregate(parent, (current, tag) => current.Element(tag)!);

    private MemoryStream ToStream(Recipe recipe)
    {
        var stream = new MemoryStream();
        _serializer.Export([recipe], stream);
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream ToStream(string content) => new(Encoding.UTF8.GetBytes(content));
}
