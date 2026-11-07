namespace BrewLog.UI.Features.Recipes;

/// <summary>Група складу в прев'ю: «Солод», «Хміль», «Дріжджі» або «Додатки».</summary>
public sealed record IngredientGroup(string Title, IReadOnlyList<IngredientLine> Lines);
