using BrewLog.Domain.Ibu;

namespace BrewLog.Domain.Recipes;

/// <summary>Рахує показники рецепта: кожен інгредієнт сам додає внесок, а формулу IBU обирає код рецепта.</summary>
public static class RecipeCalculator
{
    public static RecipeStats Calculate(Recipe recipe)
    {
        var stats = new RecipeStats(recipe.BatchVolumeL, recipe.EfficiencyPercent);

        foreach (var ingredient in recipe.Ingredients)
        {
            ingredient.ApplyTo(stats);
        }

        stats.Finish(IbuFormulaFactory.FromCode(recipe.IbuFormulaCode));
        return stats;
    }
}
