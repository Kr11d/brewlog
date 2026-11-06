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

    /// <summary>Повертає список помилок українською; порожній список означає, що стиль коректний.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Code))
        {
            errors.Add("Вкажіть код стилю.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Вкажіть назву стилю.");
        }

        (string Parameter, ValueRange Range)[] ranges =
        [
            ("OG", OgRange),
            ("FG", FgRange),
            ("ABV", AbvRange),
            ("IBU", IbuRange),
            ("SRM", SrmRange),
        ];
        errors.AddRange(ranges
            .Where(item => !(item.Range.Min <= item.Range.Max))
            .Select(item => $"{item.Parameter}: нижня межа не може бути більшою за верхню."));

        return errors;
    }
}
