using BrewLog.Domain.Common;

namespace BrewLog.Domain.Recipes;

/// <summary>Стиль пива з настанов BJCP: код, назва й допустимі діапазони показників.</summary>
public sealed class BeerStyle : IEntity
{
    public int Id { get; set; }

    /// <summary>Код BJCP, наприклад «21A».</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public ValueRange OgRange { get; set; }

    public ValueRange FgRange { get; set; }

    public ValueRange AbvRange { get; set; }

    public ValueRange IbuRange { get; set; }

    public ValueRange SrmRange { get; set; }
}
