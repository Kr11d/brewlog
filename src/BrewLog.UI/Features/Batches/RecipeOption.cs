using BrewLog.Domain.Recipes;

namespace BrewLog.UI.Features.Batches;

/// <summary>Рецепт у списку нового плану партії; Title містить назву, OG і ABV.</summary>
public sealed record RecipeOption(Recipe Recipe, string Title);
