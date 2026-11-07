using BrewLog.Domain.Recipes;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.Recipes;

/// <summary>
/// Рядок таблиці рецептів і дані його прев'ю. Числа рахує RecipeService, тут вони лише форматуються;
/// сирі значення лишаються для сортування колонок.
/// </summary>
public sealed class RecipeRowViewModel
{
    private IReadOnlyList<IngredientGroup>? _groups;

    public RecipeRowViewModel(Recipe recipe, BeerStyle? style, RecipeStats stats, decimal costPerLiter)
    {
        Recipe = recipe;
        Style = style;
        Og = stats.Og;
        Fg = stats.Fg;
        Abv = stats.Abv;
        Ibu = stats.Ibu;
        Srm = stats.Srm;
        Ebc = stats.Ebc;
        CostPerLiter = costPerLiter;
    }

    public Recipe Recipe { get; }

    public BeerStyle? Style { get; }

    public int Id => Recipe.Id;

    public string Name => Recipe.Name;

    public string StyleText => Style is null ? "Без стилю" : $"{Style.Code} {Style.Name}";

    public double Og { get; }

    public double Fg { get; }

    public double Abv { get; }

    public double Ibu { get; }

    public double Srm { get; }

    public double Ebc { get; }

    public decimal CostPerLiter { get; }

    public string OgText => DisplayFormat.Gravity(Og);

    public string FgText => DisplayFormat.Gravity(Fg);

    public string OgToFgText => $"{OgText} → {FgText}";

    public string AbvText => DisplayFormat.Abv(Abv);

    public string IbuText => DisplayFormat.Integer(Ibu);

    public string EbcText => DisplayFormat.Integer(Ebc);

    public string CostText => DisplayFormat.MoneyValue(CostPerLiter);

    public string CostPerLiterText => $"{DisplayFormat.Money(CostPerLiter)}/л";

    public string? Notes => Recipe.Notes;

    public bool HasNotes => !string.IsNullOrWhiteSpace(Recipe.Notes);

    /// <summary>Склад за групами; порожні групи пропускаються. Тип рядків обирає узагальнений IngredientsOf&lt;T&gt;, а не перевірки типу.</summary>
    public IReadOnlyList<IngredientGroup> Groups => _groups ??= new[]
        {
            Group("Солод", Recipe.IngredientsOf<Malt>()),
            Group("Хміль", Recipe.IngredientsOf<Hop>()),
            Group("Дріжджі", Recipe.IngredientsOf<Yeast>()),
            Group("Додатки", Recipe.IngredientsOf<Adjunct>()),
        }
        .Where(group => group.Lines.Count > 0)
        .ToList();

    /// <summary>Чи підходить рецепт під пошук за назвою та фільтр стилю.</summary>
    public bool Matches(string search, int? styleId) =>
        Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) && (styleId is null || styleId == Recipe.StyleId);

    private static IngredientGroup Group(string title, IEnumerable<Ingredient> lines) =>
        new(title, lines.Select(line => new IngredientLine(line.Name, DisplayFormat.Amount(line.Amount, line.Unit))).ToList());
}
