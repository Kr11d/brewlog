namespace BrewLog.Domain.Recipes;

/// <summary>
/// Плоскі дані інгредієнта будь-якого типу: так він передається між БД, BeerXML і редактором.
/// Поля, яких немає в цього типу, лишаються порожніми. Об'єкт потрібного класу створює IngredientFactory.
/// </summary>
public sealed record IngredientData(IngredientKind Kind)
{
    public int Id { get; init; }

    public int CatalogId { get; init; }

    public string Name { get; init; } = string.Empty;

    public double Amount { get; init; }

    public double? YieldPercent { get; init; }

    public double? ColorLovibond { get; init; }

    public bool? IsMashed { get; init; }

    public double? AlphaPercent { get; init; }

    public double? TimeMin { get; init; }

    public HopUse? Use { get; init; }

    public double? AttenuationPercent { get; init; }

    public double? TempMinC { get; init; }

    public double? TempMaxC { get; init; }
}
