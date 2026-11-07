using BrewLog.Domain.Recipes;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Варіант випадного списку стилів; Style = null означає «Без стилю».</summary>
public sealed record StyleOption(BeerStyle? Style, string Title);
