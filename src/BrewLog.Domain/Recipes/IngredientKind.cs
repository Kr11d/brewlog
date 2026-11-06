namespace BrewLog.Domain.Recipes;

/// <summary>Тип інгредієнта. Назви збігаються зі значеннями колонки kind у БД.</summary>
public enum IngredientKind
{
    Malt,
    Hop,
    Yeast,
    Adjunct,
}
