using BrewLog.Domain.Common;

namespace BrewLog.Domain.Recipes;

/// <summary>Стиль пива з настанов BJCP: код, назва й допустимі діапазони показників.</summary>
public sealed class BeerStyle : IEntity
{
    private static readonly ValueRange OgLimits = new(1.000, 1.200);
    private static readonly ValueRange FgLimits = new(0.990, 1.100);
    private static readonly ValueRange AbvLimits = new(0, 25);
    private static readonly ValueRange IbuLimits = new(0, 150);
    private static readonly ValueRange SrmLimits = new(0, 100);

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

    public BeerStyle Clone() => (BeerStyle)MemberwiseClone();

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

        if (string.IsNullOrWhiteSpace(Category))
        {
            errors.Add("Вкажіть категорію стилю.");
        }

        (string Parameter, ValueRange Range, ValueRange Limits, string LimitsText)[] ranges =
        [
            ("OG", OgRange, OgLimits, "від 1,000 до 1,200"),
            ("FG", FgRange, FgLimits, "від 0,990 до 1,100"),
            ("ABV", AbvRange, AbvLimits, "від 0 до 25 %"),
            ("IBU", IbuRange, IbuLimits, "від 0 до 150"),
            ("SRM", SrmRange, SrmLimits, "від 0 до 100"),
        ];
        foreach (var (parameter, range, limits, limitsText) in ranges)
        {
            if (!limits.Contains(range.Min) || !limits.Contains(range.Max))
            {
                errors.Add($"{parameter}: межі мають бути {limitsText}.");
            }
            else if (range.Min > range.Max)
            {
                errors.Add($"{parameter}: нижня межа не може бути більшою за верхню.");
            }
        }

        return errors;
    }
}
