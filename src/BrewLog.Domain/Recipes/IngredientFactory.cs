using BrewLog.Domain.Common;

namespace BrewLog.Domain.Recipes;

/// <summary>Фабрика інгредієнтів: єдине місце, де тип обирається через switch (патерн Factory).</summary>
public static class IngredientFactory
{
    /// <summary>Створює новий порожній інгредієнт: характеристики заповнює користувач.</summary>
    public static Ingredient Create(IngredientKind kind) => kind switch
    {
        IngredientKind.Malt => new Malt(),
        IngredientKind.Hop => new Hop(),
        IngredientKind.Yeast => new Yeast(),
        IngredientKind.Adjunct => new Adjunct(),
        _ => throw new DomainException($"Невідомий тип інгредієнта: {kind}."),
    };

    /// <summary>Створює інгредієнт потрібного класу з даних БД, BeerXML чи редактора.</summary>
    public static Ingredient FromData(IngredientData data)
    {
        Ingredient ingredient = data.Kind switch
        {
            IngredientKind.Malt => new Malt
            {
                YieldPercent = data.YieldPercent ?? 0,
                ColorLovibond = data.ColorLovibond ?? 0,
                IsMashed = data.IsMashed ?? true,
            },
            IngredientKind.Hop => new Hop
            {
                AlphaPercent = data.AlphaPercent ?? 0,
                TimeMin = data.TimeMin ?? 0,
                Use = data.Use ?? HopUse.Boil,
            },
            IngredientKind.Yeast => new Yeast
            {
                AttenuationPercent = data.AttenuationPercent ?? 0,
                TempMinC = data.TempMinC ?? 0,
                TempMaxC = data.TempMaxC ?? 0,
            },
            IngredientKind.Adjunct => new Adjunct
            {
                YieldPercent = data.YieldPercent ?? 0,
            },
            _ => throw new DomainException($"Невідомий тип інгредієнта: {data.Kind}."),
        };

        ingredient.Id = data.Id;
        ingredient.CatalogId = data.CatalogId;
        ingredient.Name = data.Name;
        ingredient.Amount = data.Amount;
        return ingredient;
    }
}
