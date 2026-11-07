using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Ibu;
using BrewLog.Domain.Recipes;

namespace BrewLog.Data.BeerXml;

/// <summary>
/// Обмін рецептами у форматі BeerXML 1.0. Числа завжди в InvariantCulture: файл читають інші програми й інші системи.
/// Об'єкти інгредієнтів створює лише IngredientFactory, а при експорті тип рядків обирає узагальнений IngredientsOf&lt;T&gt;.
/// Структуру файлу перевіряє цей клас, а правила рецепта — Recipe.Validate у RecipeService.
/// </summary>
public sealed class BeerXmlSerializer : IBeerXmlSerializer
{
    private const int FormatVersion = 1;
    private const double GramsPerKilogram = 1000;
    private const int GramsPrecision = 6;
    private const double DefaultYeastPacks = 1;

    private const string GrainType = "Grain";
    private const string ExtractType = "Extract";
    private const string SugarType = "Sugar";
    private const string BoilUse = "Boil";
    private const string DryHopUse = "Dry Hop";

    /// <summary>Тип FERMENTABLE визначає клас інгредієнта й те, чи діє на нього ефективність затирання.</summary>
    private static readonly Dictionary<string, (IngredientKind Kind, bool? IsMashed)> FermentableTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [GrainType] = (IngredientKind.Malt, true),
        [ExtractType] = (IngredientKind.Malt, false),
        ["Dry Extract"] = (IngredientKind.Malt, false),
        [SugarType] = (IngredientKind.Adjunct, null),
        ["Adjunct"] = (IngredientKind.Adjunct, null),
    };

    private readonly IStyleRepository _styles;

    /// <param name="styles">Довідник стилів: файл зберігає код стилю, а рецепт — ідентифікатор.</param>
    public BeerXmlSerializer(IStyleRepository styles)
    {
        _styles = styles;
    }

    public IReadOnlyList<Recipe> Import(Stream stream)
    {
        var root = LoadRoot(stream);
        var styles = _styles.GetAll();

        var recipes = root.Elements("RECIPE").Select(element => ReadRecipe(element, styles)).ToList();
        if (recipes.Count == 0)
        {
            throw new DomainException("У файлі BeerXML немає жодного рецепта.");
        }

        return recipes;
    }

    public void Export(IEnumerable<Recipe> recipes, Stream stream)
    {
        var styles = _styles.GetAll();
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("RECIPES", recipes.Select(recipe => WriteRecipe(recipe, styles))));

        using var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) });
        document.Save(writer);
    }

    /// <summary>DTD заборонено: файл приходить іззовні, а зовнішні сутності могли б читати чужі дані чи вичерпати пам'ять.</summary>
    private static XElement LoadRoot(Stream stream)
    {
        try
        {
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            var root = XDocument.Load(reader).Root;
            if (root is null || root.Name != "RECIPES")
            {
                throw new DomainException("Це не файл BeerXML: немає елемента RECIPES.");
            }

            return root;
        }
        catch (XmlException exception)
        {
            throw new DomainException($"Файл не є коректним XML: помилка в рядку {exception.LineNumber}, позиція {exception.LinePosition}.", exception);
        }
    }

    private static Recipe ReadRecipe(XElement element, IReadOnlyList<BeerStyle> styles)
    {
        var recipe = new Recipe
        {
            Name = ReadText(element, "NAME"),
            StyleId = FindStyleId(element.Element("STYLE"), styles),
            BatchVolumeL = ReadRequired(element, "BATCH_SIZE"),
            IbuFormulaCode = FindFormulaCode(ReadText(element, "IBU_METHOD")),
        };

        recipe.EfficiencyPercent = ReadOptional(element, "EFFICIENCY") ?? recipe.EfficiencyPercent;
        recipe.BoilTimeMin = (int)Math.Round(ReadOptional(element, "BOIL_TIME") ?? recipe.BoilTimeMin);

        var notes = ReadText(element, "NOTES");
        recipe.Notes = notes.Length == 0 ? null : notes;

        var lines = Records(element, "FERMENTABLES", "FERMENTABLE").Select(ReadFermentable)
            .Concat(Records(element, "HOPS", "HOP").Select(ReadHop))
            .Concat(Records(element, "YEASTS", "YEAST").Select(ReadYeast));
        foreach (var line in lines)
        {
            recipe.AddIngredient(IngredientFactory.FromData(line));
        }

        return recipe;
    }

    /// <summary>Код стилю складається з CATEGORY_NUMBER і STYLE_LETTER; невідомий стиль лишає рецепт без стилю.</summary>
    private static int? FindStyleId(XElement? style, IReadOnlyList<BeerStyle> styles)
    {
        if (style is null)
        {
            return null;
        }

        var code = ReadText(style, "CATEGORY_NUMBER") + ReadText(style, "STYLE_LETTER");
        return styles.FirstOrDefault(item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase))?.Id;
    }

    /// <summary>IBU_METHOD необов'язковий, а формула, якої BrewLog не має, замінюється типовою.</summary>
    private static string FindFormulaCode(string method) =>
        IbuFormulaFactory.All.FirstOrDefault(formula => string.Equals(formula.Code, method, StringComparison.OrdinalIgnoreCase))?.Code
        ?? IbuFormulaFactory.Default.Code;

    private static IngredientData ReadFermentable(XElement element)
    {
        var type = ReadText(element, "TYPE");
        if (!FermentableTypes.TryGetValue(type, out var mapping))
        {
            throw new DomainException($"Файл BeerXML: невідомий тип FERMENTABLE «{type}».");
        }

        return new IngredientData(mapping.Kind)
        {
            Name = ReadText(element, "NAME"),
            Amount = ReadRequired(element, "AMOUNT"),
            YieldPercent = ReadRequired(element, "YIELD"),
            ColorLovibond = ReadRequired(element, "COLOR"),
            IsMashed = mapping.IsMashed,
        };
    }

    /// <summary>Файл зберігає хміль у кілограмах. Заокруглення прибирає похибку double: 0,07 · 1000 дає 70,00000000000001.</summary>
    private static IngredientData ReadHop(XElement element) => new(IngredientKind.Hop)
    {
        Name = ReadText(element, "NAME"),
        Amount = Math.Round(ReadRequired(element, "AMOUNT") * GramsPerKilogram, GramsPrecision),
        AlphaPercent = ReadRequired(element, "ALPHA"),
        TimeMin = ReadRequired(element, "TIME"),
        Use = string.Equals(ReadText(element, "USE"), DryHopUse, StringComparison.OrdinalIgnoreCase) ? HopUse.DryHop : HopUse.Boil,
    };

    /// <summary>
    /// За стандартом AMOUNT дріжджів — літри чи кілограми, зазвичай менше 1, а BrewLog пише сюди кількість пакетів.
    /// Тому значення менше 1 або його відсутність означає один пакет. Решта полів необов'язкові: їх перевірить Recipe.Validate.
    /// </summary>
    private static IngredientData ReadYeast(XElement element)
    {
        var amount = ReadOptional(element, "AMOUNT");
        return new IngredientData(IngredientKind.Yeast)
        {
            Name = ReadText(element, "NAME"),
            Amount = amount >= DefaultYeastPacks ? amount.Value : DefaultYeastPacks,
            AttenuationPercent = ReadOptional(element, "ATTENUATION"),
            TempMinC = ReadOptional(element, "MIN_TEMPERATURE"),
            TempMaxC = ReadOptional(element, "MAX_TEMPERATURE"),
        };
    }

    private static IEnumerable<XElement> Records(XElement recipe, string list, string record) =>
        recipe.Element(list)?.Elements(record) ?? [];

    private static string ReadText(XElement parent, string tag) => parent.Element(tag)?.Value.Trim() ?? string.Empty;

    private static double ReadRequired(XElement parent, string tag) =>
        ReadOptional(parent, tag) ?? throw new DomainException($"Файл BeerXML: у записі {parent.Name} немає поля {tag}.");

    private static double? ReadOptional(XElement parent, string tag)
    {
        var text = ReadText(parent, tag);
        if (text.Length == 0)
        {
            return null;
        }

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !NumberLimits.IsAllowed(value))
        {
            throw new DomainException($"Файл BeerXML: некоректне число в полі {tag}: «{text}».");
        }

        return value;
    }

    /// <summary>MISCS, WATERS і MASH стандарт вимагає, але BrewLog їх не веде, тому вони порожні.</summary>
    private static XElement WriteRecipe(Recipe recipe, IReadOnlyList<BeerStyle> styles) => new(
        "RECIPE",
        new XElement("NAME", recipe.Name),
        Version(),
        new XElement("TYPE", "All Grain"),
        WriteStyle(styles.FirstOrDefault(style => style.Id == recipe.StyleId)),
        new XElement("BREWER", "BrewLog"),
        Number("BATCH_SIZE", recipe.BatchVolumeL),
        Number("BOIL_SIZE", recipe.BatchVolumeL),
        Number("BOIL_TIME", recipe.BoilTimeMin),
        Number("EFFICIENCY", recipe.EfficiencyPercent),
        new XElement("HOPS", recipe.IngredientsOf<Hop>().Select(WriteHop)),
        new XElement(
            "FERMENTABLES",
            recipe.IngredientsOf<Malt>().Select(WriteMalt),
            recipe.IngredientsOf<Adjunct>().Select(WriteAdjunct)),
        new XElement("MISCS"),
        new XElement("YEASTS", recipe.IngredientsOf<Yeast>().Select(WriteYeast)),
        new XElement("WATERS"),
        new XElement("MASH", Version()),
        new XElement("IBU_METHOD", recipe.IbuFormulaCode),
        new XElement("NOTES", recipe.Notes));

    /// <summary>Тип стилю стандарт вимагає, але BrewLog його не зберігає; «Mixed» не стверджує зайвого.</summary>
    private static XElement WriteStyle(BeerStyle? style)
    {
        if (style is null)
        {
            return new XElement("STYLE", Version());
        }

        var categoryNumber = new string(style.Code.TakeWhile(char.IsDigit).ToArray());
        return new XElement(
            "STYLE",
            new XElement("NAME", style.Name),
            Version(),
            new XElement("CATEGORY", style.Category),
            new XElement("CATEGORY_NUMBER", categoryNumber),
            new XElement("STYLE_LETTER", style.Code[categoryNumber.Length..]),
            new XElement("STYLE_GUIDE", "BJCP"),
            new XElement("TYPE", "Mixed"),
            Number("OG_MIN", style.OgRange.Min),
            Number("OG_MAX", style.OgRange.Max),
            Number("FG_MIN", style.FgRange.Min),
            Number("FG_MAX", style.FgRange.Max),
            Number("IBU_MIN", style.IbuRange.Min),
            Number("IBU_MAX", style.IbuRange.Max),
            Number("COLOR_MIN", style.SrmRange.Min),
            Number("COLOR_MAX", style.SrmRange.Max),
            Number("ABV_MIN", style.AbvRange.Min),
            Number("ABV_MAX", style.AbvRange.Max));
    }

    private static XElement WriteMalt(Malt malt) =>
        WriteFermentable(malt, malt.IsMashed ? GrainType : ExtractType, malt.YieldPercent, malt.ColorLovibond);

    /// <summary>Додаток BrewLog поводиться як цукор: дає екстракт без ефективності й не має кольору.</summary>
    private static XElement WriteAdjunct(Adjunct adjunct) =>
        WriteFermentable(adjunct, SugarType, adjunct.YieldPercent, colorLovibond: 0);

    private static XElement WriteFermentable(Ingredient line, string type, double yieldPercent, double colorLovibond) => new(
        "FERMENTABLE",
        new XElement("NAME", line.Name),
        Version(),
        new XElement("TYPE", type),
        Number("AMOUNT", line.Amount),
        Number("YIELD", yieldPercent),
        Number("COLOR", colorLovibond));

    private static XElement WriteHop(Hop hop) => new(
        "HOP",
        new XElement("NAME", hop.Name),
        Version(),
        Number("ALPHA", hop.AlphaPercent),
        Number("AMOUNT", hop.Amount / GramsPerKilogram),
        new XElement("USE", hop.Use == HopUse.DryHop ? DryHopUse : BoilUse),
        Number("TIME", hop.TimeMin));

    /// <summary>Тип і форму дріжджів стандарт вимагає, але BrewLog їх не зберігає, тому вони сталі. AMOUNT — кількість пакетів, див. ReadYeast.</summary>
    private static XElement WriteYeast(Yeast yeast) => new(
        "YEAST",
        new XElement("NAME", yeast.Name),
        Version(),
        new XElement("TYPE", "Ale"),
        new XElement("FORM", "Dry"),
        Number("AMOUNT", yeast.Amount),
        Number("ATTENUATION", yeast.AttenuationPercent),
        Number("MIN_TEMPERATURE", yeast.TempMinC),
        Number("MAX_TEMPERATURE", yeast.TempMaxC));

    private static XElement Version() => new("VERSION", FormatVersion);

    private static XElement Number(string tag, double value) => new(tag, value.ToString(CultureInfo.InvariantCulture));
}
