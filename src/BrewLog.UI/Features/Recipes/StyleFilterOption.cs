namespace BrewLog.UI.Features.Recipes;

/// <summary>Пункт фільтра за стилем; StyleId = null означає «Усі стилі».</summary>
public sealed record StyleFilterOption(int? StyleId, string Title);
