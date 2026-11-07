namespace BrewLog.Domain.Recipes;

/// <summary>Підсумок імпорту: ідентифікатори збережених рецептів і кількість нових позицій довідника.</summary>
public sealed record RecipeImportResult(IReadOnlyList<int> RecipeIds, int NewCatalogItems);
