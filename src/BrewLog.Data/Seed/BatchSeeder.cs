using System.Data;
using BrewLog.Data.Repositories;
using BrewLog.Data.Rows;
using BrewLog.Domain.Batches;
using BrewLog.Domain.Recipes;
using Dapper;

namespace BrewLog.Data.Seed;

/// <summary>
/// Вісім демо-партій за останні 6 місяців (HANDOFF §10): 4 розлиті, 1 у дозріванні, 2 у бродінні, 1 запланована.
/// Виміри щодня до вчорашнього дня: сьогоднішнього ще немає, тож журнал помічника показує, що партії пора міряти.
/// Шум температури детермінований. Собівартість записується напряму, без руху складу.
/// </summary>
public sealed class BatchSeeder : ISeeder
{
    private const double SlowDecay = 0.5;
    private const double NormalDecay = 0.7;

    public int Order => 50;

    public void Seed(IDbConnection c, IDbTransaction tx, DateTime today)
    {
        var recipes = LoadRecipes(c, tx);
        var styleNames = c.Query<StyleRow>("SELECT * FROM styles", transaction: tx).ToDictionary(row => row.Id, row => row.Name);
        var prices = c.Query<IngredientCatalogRow>("SELECT id, last_price FROM ingredient_catalog", transaction: tx)
            .ToDictionary(row => row.Id, row => (decimal)row.LastPrice);
        var brewerId = c.QuerySingle<int>("SELECT id FROM users WHERE login = 'brewer'", transaction: tx);
        var assistantId = c.QuerySingle<int>("SELECT id FROM users WHERE login = 'assistant'", transaction: tx);
        var numbers = new List<string>();

        void Save(Batch batch)
        {
            batch.Id = BatchRepository.InsertWithDetails(c, tx, batch);
            numbers.Add(batch.Number);
        }

        Batch Brewed(
            string recipeName,
            double volumeL,
            BatchState state,
            int brewedDaysAgo,
            int readingDays,
            int? conditioningAfterDays,
            bool assigned,
            double ogOffset,
            double decay = NormalDecay,
            double? lastTemperatureAboveMax = null)
        {
            var recipe = recipes[recipeName];
            var stats = RecipeCalculator.Calculate(recipe);
            var brewDay = today.AddDays(-brewedDaysAgo);
            var actualOg = Math.Round(stats.Og + ogOffset, 3);
            var workerId = assigned ? assistantId : brewerId;
            var scale = recipe.ScaleFactor(volumeL);

            return new Batch
            {
                Number = BatchNumberGenerator.Next(brewDay.Year, numbers),
                RecipeId = recipe.Id,
                RecipeName = recipe.Name,
                StyleName = styleNames[recipe.StyleId!.Value],
                VolumeL = volumeL,
                State = state,
                PlannedDate = brewDay,
                BrewedAt = brewDay.AddHours(8),
                FermentingAt = brewDay.AddHours(18),
                ConditioningAt = conditioningAfterDays is { } days ? brewDay.AddDays(days).AddHours(10) : null,
                PlannedOg = stats.Og,
                PlannedFg = stats.Fg,
                PlannedAbv = stats.Abv,
                PlannedSrm = stats.Srm,
                TempMinC = stats.TempMinC,
                TempMaxC = stats.TempMaxC,
                ActualOg = actualOg,
                IngredientsCost = recipe.Ingredients.Sum(line => (decimal)(line.Amount * scale) * prices[line.CatalogId]),
                AssistantId = assigned ? assistantId : null,
                CreatedBy = brewerId,
                Measurements = Readings(
                    brewDay.AddDays(1),
                    readingDays,
                    actualOg,
                    stats.Fg + 0.001,
                    decay,
                    stats.TempMinC!.Value,
                    stats.TempMaxC!.Value,
                    seed: brewedDaysAgo,
                    workerId,
                    lastTemperatureAboveMax),
            };
        }

        void Packaged(Batch batch, int packagedAfterConditioningDays, int bottles, int kegs)
        {
            var packagedAt = batch.ConditioningAt!.Value.AddDays(packagedAfterConditioningDays);
            batch.SetPackaging(new Packaging
            {
                PackagedAt = packagedAt,
                BottlesCount = bottles,
                KegsCount = kegs,
                FinalGravity = batch.CurrentGravity,
                UserId = batch.AssistantId ?? brewerId,
            });
            batch.Advance(new TransitionContext(batch, packagedAt, null));
            Save(batch);
        }

        Packaged(Brewed("Czech Pils", 50, new ConditioningState(), 170, 14, 14, assigned: true, 0.001), 21, bottles: 96, kegs: 0);
        Packaged(Brewed("Irish Stout", 50, new ConditioningState(), 130, 10, 10, assigned: false, -0.001), 12, bottles: 18, kegs: 2);
        Packaged(Brewed("Weissbier", 50, new ConditioningState(), 90, 10, 10, assigned: true, 0.002), 7, bottles: 56, kegs: 1);
        Packaged(Brewed("American IPA", 50, new ConditioningState(), 55, 12, 12, assigned: false, 0), 14, bottles: 16, kegs: 2);

        Save(Brewed("Czech Pils", 50, new ConditioningState(), 29, 28, 13, assigned: false, 0.001));
        Save(Brewed("American Pale Ale", 50, new FermentingState(), 11, 10, null, assigned: true, -0.001));
        Save(Brewed("Weissbier", 40, new FermentingState(), 6, 5, null, assigned: true, 0.001, SlowDecay, lastTemperatureAboveMax: 2.5));

        var ipa = recipes["American IPA"];
        var ipaStats = RecipeCalculator.Calculate(ipa);
        Save(new Batch
        {
            Number = BatchNumberGenerator.Next(today.Year, numbers),
            RecipeId = ipa.Id,
            RecipeName = ipa.Name,
            StyleName = styleNames[ipa.StyleId!.Value],
            VolumeL = 50,
            PlannedDate = today.AddDays(3),
            PlannedOg = ipaStats.Og,
            PlannedFg = ipaStats.Fg,
            PlannedAbv = ipaStats.Abv,
            PlannedSrm = ipaStats.Srm,
            TempMinC = ipaStats.TempMinC,
            TempMaxC = ipaStats.TempMaxC,
            CreatedBy = brewerId,
        });
    }

    private static Dictionary<string, Recipe> LoadRecipes(IDbConnection c, IDbTransaction tx)
    {
        var lines = c.Query<RecipeIngredientRow>("SELECT * FROM recipe_ingredients ORDER BY sort_order, id", transaction: tx)
            .ToLookup(line => line.RecipeId);

        return c.Query<RecipeRow>("SELECT * FROM recipes", transaction: tx)
            .Select(row => row.ToRecipe(lines[row.Id].Select(line => IngredientFactory.FromData(line.ToData()))))
            .ToDictionary(recipe => recipe.Name);
    }

    /// <summary>Щоденні виміри: густина падає експоненційно до кінцевої, температура — середина діапазону дріжджів із шумом ±0,5 °C.</summary>
    private static List<Measurement> Readings(
        DateTime firstDay,
        int days,
        double og,
        double finalGravity,
        double decay,
        double tempMinC,
        double tempMaxC,
        int seed,
        int userId,
        double? lastTemperatureAboveMax)
    {
        var random = new Random(seed);
        var middle = (tempMinC + tempMaxC) / 2;

        return Enumerable.Range(0, days)
            .Select(day =>
            {
                var noisy = middle + random.NextDouble() - 0.5;
                var temperature = day == days - 1 && lastTemperatureAboveMax is { } above ? tempMaxC + above : noisy;
                return new Measurement
                {
                    MeasuredAt = firstDay.AddDays(day).AddHours(9),
                    Gravity = Math.Round(finalGravity + (og - finalGravity) * Math.Exp(-decay * day), 3),
                    TemperatureC = Math.Round(temperature, 1),
                    UserId = userId,
                };
            })
            .ToList();
    }
}
