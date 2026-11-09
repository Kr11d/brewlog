using BrewLog.Domain.Common;

namespace BrewLog.Domain.Recipes;

/// <summary>Хміль: варильне внесення дає гіркоту, сухе охмелення — лише аромат.</summary>
public sealed class Hop : Ingredient
{
    private static readonly ValueRange AlphaRange = new(0, 25);

    /// <summary>Альфа-кислоти, %.</summary>
    public double AlphaPercent { get; set; }

    /// <summary>Час кипіння, хв.</summary>
    public double TimeMin { get; set; }

    public HopUse Use { get; set; } = HopUse.Boil;

    public override IngredientKind Kind => IngredientKind.Hop;

    public override string Unit => "г";

    public override string KindDisplayName => "Хміль";

    public override double PurchaseStep => 50;

    /// <summary>Реєструє варильне внесення: IBU рахується в кінці, бо залежить від OG усього рецепта. Сухе охмелення гіркоти не додає.</summary>
    public override void ApplyTo(RecipeStats stats)
    {
        if (Use == HopUse.Boil)
        {
            stats.AddBoilHop(this);
        }
    }

    /// <summary>Наприклад, «18,4 IBU».</summary>
    public override string DescribeContribution(RecipeStats final) => $"{FormatNumber(final.IbuOf(this), "0.0")} IBU";

    public override IngredientData ToData() => base.ToData() with
    {
        AlphaPercent = AlphaPercent,
        TimeMin = TimeMin,
        Use = Use,
    };

    public override Hop Clone() => (Hop)MemberwiseClone();

    protected override IEnumerable<string> ValidateCharacteristics()
    {
        if (!AlphaRange.Contains(AlphaPercent))
        {
            yield return $"{Label}: альфа-кислоти мають бути від 0 до 25 %.";
        }

        if (!(TimeMin >= 0))
        {
            yield return $"{Label}: час не може бути від'ємним.";
        }
    }
}
