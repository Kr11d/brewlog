using System.Data;
using BrewLog.Data.Rows;
using BrewLog.Domain.Recipes;
using Dapper;

namespace BrewLog.Data.Seed;

/// <summary>
/// Демо-довідники: 15 стилів з діапазонами настанов BJCP 2021, сировина й двоє вигаданих постачальників.
/// Зброджування дріжджів задане як фактичне у варці, а не лабораторне з паспорта виробника:
/// формула FG бере його напряму, і з паспортними 80–90 % лагери й пшеничне вийшли б сухішими за свій стиль.
/// </summary>
public sealed class CatalogSeeder : ISeeder
{
    public int Order => 20;

    public void Seed(IDbConnection c, IDbTransaction tx, DateTime today)
    {
        c.Execute(StyleRow.InsertSql, Styles(), tx);
        c.Execute(IngredientCatalogRow.InsertSql, Ingredients(), tx);
        c.Execute(
            "INSERT INTO suppliers (name, phone, email) VALUES (@Name, @Phone, @Email);",
            new[]
            {
                new { Name = "ТОВ «Солодова справа»", Phone = "+380 44 000 00 01", Email = "sales@solodova.example.com" },
                new { Name = "ФОП «Хмільний двір»", Phone = "+380 67 000 00 02", Email = "order@hmilnyi.example.com" },
            },
            tx);
    }

    private static IEnumerable<StyleRow> Styles() =>
    [
        Style("1B", "American Lager", "Standard American Beer", 1.040, 1.050, 1.004, 1.010, 4.2, 5.3, 8, 18, 2, 3.5),
        Style("3B", "Czech Premium Pale Lager", "Czech Lager", 1.044, 1.060, 1.013, 1.017, 4.2, 5.8, 30, 45, 3.5, 6),
        Style("4A", "Munich Helles", "Pale Malty European Lager", 1.044, 1.048, 1.006, 1.012, 4.7, 5.4, 16, 22, 3, 5),
        Style("5D", "German Pils", "Pale Bitter European Beer", 1.044, 1.050, 1.008, 1.013, 4.4, 5.2, 22, 40, 2, 4),
        Style("10A", "Weissbier", "German Wheat Beer", 1.044, 1.053, 1.008, 1.014, 4.3, 5.6, 8, 15, 2, 6),
        Style("11A", "Ordinary Bitter", "British Bitter", 1.030, 1.039, 1.007, 1.011, 3.2, 3.8, 25, 35, 8, 14),
        Style("12C", "English IPA", "Pale Commonwealth Beer", 1.050, 1.070, 1.010, 1.015, 5.0, 7.5, 40, 60, 6, 14),
        Style("15B", "Irish Stout", "Irish Beer", 1.036, 1.044, 1.007, 1.011, 3.8, 5.0, 25, 45, 25, 40),
        Style("16A", "Sweet Stout", "Dark British Beer", 1.044, 1.060, 1.012, 1.024, 4.0, 6.0, 20, 40, 30, 40),
        Style("18B", "American Pale Ale", "Pale American Ale", 1.045, 1.060, 1.010, 1.015, 4.5, 6.2, 30, 50, 5, 10),
        Style("19A", "American Amber Ale", "Amber and Brown American Beer", 1.045, 1.060, 1.010, 1.015, 4.5, 6.2, 25, 40, 10, 17),
        Style("20A", "American Porter", "American Porter and Stout", 1.050, 1.070, 1.012, 1.018, 4.8, 6.5, 25, 50, 22, 40),
        Style("21A", "American IPA", "IPA", 1.056, 1.070, 1.008, 1.014, 5.5, 7.5, 40, 70, 6, 14),
        Style("21C", "Hazy IPA", "IPA", 1.060, 1.085, 1.010, 1.015, 6.0, 9.0, 25, 60, 3, 7),
        Style("24A", "Witbier", "Belgian Ale", 1.044, 1.052, 1.008, 1.012, 4.5, 5.5, 8, 20, 2, 4),
    ];

    private static IEnumerable<IngredientCatalogRow> Ingredients() =>
    [
        Malt("Pilsner", yieldPercent: 80, lovibond: 1.8, minStock: 25, price: 38m),
        Malt("Pale Ale", yieldPercent: 80, lovibond: 3, minStock: 25, price: 42m),
        Malt("Munich", yieldPercent: 78, lovibond: 9, minStock: 10, price: 48m),
        Malt("Vienna", yieldPercent: 79, lovibond: 3.5, minStock: 10, price: 46m),
        Malt("Wheat", yieldPercent: 81, lovibond: 2.5, minStock: 10, price: 44m),
        Malt("Carapils", yieldPercent: 72, lovibond: 1.5, minStock: 5, price: 62m),
        Malt("Crystal 60", yieldPercent: 74, lovibond: 60, minStock: 5, price: 68m),
        Malt("Chocolate", yieldPercent: 60, lovibond: 350, minStock: 3, price: 78m),
        Malt("Roasted Barley", yieldPercent: 55, lovibond: 450, minStock: 3, price: 74m),
        Malt("Вівсяні пластівці", yieldPercent: 70, lovibond: 1, minStock: 3, price: 40m),

        Hop("Saaz", alphaPercent: 3.5, minStock: 500, price: 1.60m),
        Hop("Hallertauer Mittelfrüh", alphaPercent: 4, minStock: 300, price: 1.80m),
        Hop("East Kent Goldings", alphaPercent: 5, minStock: 300, price: 2.00m),
        Hop("Magnum", alphaPercent: 13, minStock: 200, price: 1.40m),
        Hop("Cascade", alphaPercent: 5.5, minStock: 300, price: 1.90m),
        Hop("Citra", alphaPercent: 12, minStock: 200, price: 3.20m),
        Hop("Mosaic", alphaPercent: 12, minStock: 200, price: 3.40m),

        Yeast("US-05", attenuationPercent: 77, tempMinC: 15, tempMaxC: 24, minStock: 5, price: 165m),
        Yeast("S-04", attenuationPercent: 75, tempMinC: 15, tempMaxC: 24, minStock: 4, price: 155m),
        Yeast("W-34/70", attenuationPercent: 75, tempMinC: 9, tempMaxC: 15, minStock: 4, price: 210m),
        Yeast("WB-06", attenuationPercent: 76, tempMinC: 15, tempMaxC: 24, minStock: 3, price: 160m),
        Yeast("Nottingham", attenuationPercent: 77, tempMinC: 10, tempMaxC: 22, minStock: 3, price: 170m),

        Adjunct("Сахароза", yieldPercent: 100, minStock: 5, price: 32m),
        Adjunct("Декстроза", yieldPercent: 100, minStock: 3, price: 55m),
    ];

    private static StyleRow Style(
        string code,
        string name,
        string category,
        double ogMin,
        double ogMax,
        double fgMin,
        double fgMax,
        double abvMin,
        double abvMax,
        double ibuMin,
        double ibuMax,
        double srmMin,
        double srmMax) => new()
    {
        Code = code,
        Name = name,
        Category = category,
        OgMin = ogMin,
        OgMax = ogMax,
        FgMin = fgMin,
        FgMax = fgMax,
        AbvMin = abvMin,
        AbvMax = abvMax,
        IbuMin = ibuMin,
        IbuMax = ibuMax,
        SrmMin = srmMin,
        SrmMax = srmMax,
    };

    private static IngredientCatalogRow Malt(string name, double yieldPercent, double lovibond, double minStock, decimal price) =>
        IngredientCatalogRow.FromData(
            new IngredientData(IngredientKind.Malt) { Name = name, YieldPercent = yieldPercent, ColorLovibond = lovibond, IsMashed = true },
            minStock,
            price);

    private static IngredientCatalogRow Hop(string name, double alphaPercent, double minStock, decimal price) =>
        IngredientCatalogRow.FromData(
            new IngredientData(IngredientKind.Hop) { Name = name, AlphaPercent = alphaPercent },
            minStock,
            price);

    private static IngredientCatalogRow Yeast(string name, double attenuationPercent, double tempMinC, double tempMaxC, double minStock, decimal price) =>
        IngredientCatalogRow.FromData(
            new IngredientData(IngredientKind.Yeast) { Name = name, AttenuationPercent = attenuationPercent, TempMinC = tempMinC, TempMaxC = tempMaxC },
            minStock,
            price);

    private static IngredientCatalogRow Adjunct(string name, double yieldPercent, double minStock, decimal price) =>
        IngredientCatalogRow.FromData(
            new IngredientData(IngredientKind.Adjunct) { Name = name, YieldPercent = yieldPercent },
            minStock,
            price);
}
