using BrewLog.Domain.Recipes;

namespace BrewLog.Tests.Recipes;

/// <summary>Коректні інгредієнти з еталонів HANDOFF §6.1; тест змінює лише те, що перевіряє.</summary>
public static class SampleIngredients
{
    public static Malt Pilsner() => new()
    {
        CatalogId = 1,
        Name = "Pilsner",
        Amount = 5,
        YieldPercent = 80,
        ColorLovibond = 1.8,
        IsMashed = true,
    };

    public static Adjunct Sucrose(double kilograms = 0.5) => new()
    {
        CatalogId = 2,
        Name = "Сахароза",
        Amount = kilograms,
        YieldPercent = 100,
    };

    public static Hop BoilHop() => new()
    {
        CatalogId = 3,
        Name = "Magnum",
        Amount = 30,
        AlphaPercent = 10,
        TimeMin = 60,
        Use = HopUse.Boil,
    };

    public static Hop DryHop() => new()
    {
        CatalogId = 5,
        Name = "Citra",
        Amount = 50,
        AlphaPercent = 12,
        TimeMin = 5,
        Use = HopUse.DryHop,
    };

    public static Yeast Us05(double attenuationPercent = 77) => new()
    {
        CatalogId = 4,
        Name = "US-05",
        Amount = 1,
        AttenuationPercent = attenuationPercent,
        TempMinC = 15,
        TempMaxC = 24,
    };

    public static Recipe RecipeOf(double volumeL, params Ingredient[] ingredients)
    {
        var recipe = new Recipe { Name = "Тестовий рецепт", BatchVolumeL = volumeL };
        foreach (var ingredient in ingredients)
        {
            recipe.AddIngredient(ingredient);
        }

        return recipe;
    }

    /// <summary>Рецепт з OG рівно 1,050: 1 кг сахарози дає 385,6 пункта·л, тобто 50 пунктів на 7,712 л.</summary>
    public static Recipe OgFiftyRecipe(params Ingredient[] others) =>
        RecipeOf(7.712, [Sucrose(kilograms: 1), .. others]);
}
