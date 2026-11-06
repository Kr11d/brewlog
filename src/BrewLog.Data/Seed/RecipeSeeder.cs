using System.Data;
using BrewLog.Data.Repositories;
using BrewLog.Data.Rows;
using BrewLog.Domain.Ibu;
using BrewLog.Domain.Recipes;
using Dapper;

namespace BrewLog.Data.Seed;

/// <summary>
/// П'ять демо-рецептів на 50 л. Показники кожного потрапляють у діапазони свого стилю: це перевіряє тест DemoDataTests,
/// а значення видно в журналі smoke-перевірки.
/// </summary>
public sealed class RecipeSeeder : ISeeder
{
    public int Order => 30;

    public void Seed(IDbConnection c, IDbTransaction tx, DateTime today)
    {
        var catalog = c.Query<IngredientCatalogRow>("SELECT * FROM ingredient_catalog", transaction: tx)
            .ToDictionary(row => row.Name, row => row.ToData());
        var styleIds = c.Query<StyleRow>("SELECT * FROM styles", transaction: tx)
            .ToDictionary(row => row.Code, row => row.Id);

        Ingredient Line(string name, double amount) =>
            IngredientFactory.FromData(catalog[name] with { Id = 0, Amount = amount });

        Ingredient Boil(string name, double grams, double minutes) =>
            IngredientFactory.FromData(catalog[name] with { Id = 0, Amount = grams, TimeMin = minutes, Use = HopUse.Boil });

        Ingredient DryHop(string name, double grams) =>
            IngredientFactory.FromData(catalog[name] with { Id = 0, Amount = grams, TimeMin = 0, Use = HopUse.DryHop });

        void Add(string name, string styleCode, string formulaCode, string notes, params Ingredient[] lines)
        {
            var createdAt = today.AddMonths(-8);
            var recipe = new Recipe
            {
                Name = name,
                StyleId = styleIds[styleCode],
                BatchVolumeL = 50,
                IbuFormulaCode = formulaCode,
                Notes = notes,
                CreatedAt = createdAt,
                UpdatedAt = createdAt,
            };

            foreach (var line in lines)
            {
                recipe.AddIngredient(line);
            }

            RecipeRepository.InsertWithLines(c, tx, recipe);
        }

        var tinseth = new TinsethFormula().Code;
        var rager = new RagerFormula().Code;

        Add(
            "Czech Pils",
            "3B",
            tinseth,
            "Класичний чеський лагер: виразний Saaz і м'яка солодова основа.",
            Line("Pilsner", 10.5),
            Line("Munich", 1),
            Line("Carapils", 0.8),
            Boil("Saaz", grams: 200, minutes: 60),
            Boil("Saaz", grams: 60, minutes: 15),
            Line("W-34/70", 4));

        Add(
            "American IPA",
            "21A",
            tinseth,
            "Цитрусовий аромат від пізнього внесення й сухого охмелення Citra та Mosaic.",
            Line("Pale Ale", 12.2),
            Line("Crystal 60", 0.8),
            Line("Carapils", 0.4),
            Boil("Magnum", grams: 60, minutes: 60),
            Boil("Citra", grams: 60, minutes: 15),
            Boil("Mosaic", grams: 60, minutes: 10),
            DryHop("Citra", grams: 100),
            DryHop("Mosaic", grams: 100),
            Line("US-05", 3));

        Add(
            "Irish Stout",
            "15B",
            tinseth,
            "Сухий стаут: смак кави дає смажений ячмінь, а вівсяні пластівці — повноту.",
            Line("Pale Ale", 7.8),
            Line("Вівсяні пластівці", 1),
            Line("Roasted Barley", 1),
            Boil("East Kent Goldings", grams: 140, minutes: 60),
            Line("S-04", 3));

        Add(
            "Weissbier",
            "10A",
            tinseth,
            "Баварське пшеничне: банан і гвоздика походять від дріжджів, гіркоти небагато.",
            Line("Wheat", 6),
            Line("Pilsner", 4.7),
            Boil("Hallertauer Mittelfrüh", grams: 65, minutes: 60),
            Line("WB-06", 3));

        Add(
            "American Pale Ale",
            "18B",
            rager,
            "Збалансований ель із карамельним тоном і квітковим ароматом Cascade.",
            Line("Pale Ale", 10.5),
            Line("Munich", 0.8),
            Line("Crystal 60", 0.5),
            Boil("Magnum", grams: 25, minutes: 60),
            Boil("Cascade", grams: 70, minutes: 30),
            Boil("Cascade", grams: 80, minutes: 5),
            Line("US-05", 3));
    }
}
