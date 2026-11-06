using BrewLog.Domain.Common;

namespace BrewLog.Domain.Recipes;

/// <summary>Солод або солодовий екстракт: дає екстракт і колір.</summary>
public sealed class Malt : Ingredient
{
    private static readonly ValueRange YieldRange = new(0, 100);

    /// <summary>Вихід екстракту, %.</summary>
    public double YieldPercent { get; set; }

    /// <summary>Колір, градуси Ловібонда.</summary>
    public double ColorLovibond { get; set; }

    /// <summary>true — зерно, яке затирається (діє ефективність варки); false — готовий екстракт.</summary>
    public bool IsMashed { get; set; } = true;

    public override IngredientKind Kind => IngredientKind.Malt;

    public override string Unit => "кг";

    /// <summary>Додає екстракт (ефективність діє лише на затерте зерно) і колір (MCU).</summary>
    public override void ApplyTo(RecipeStats stats)
    {
        stats.AddExtract(this, YieldPercent, usesEfficiency: IsMashed);
        stats.AddColor(this, ColorLovibond);
    }

    /// <summary>Наприклад, «+0,031 OG · 4,2 SRM».</summary>
    public override string DescribeContribution(RecipeStats final) =>
        $"{base.DescribeContribution(final)} · {FormatNumber(final.SrmOf(this), "0.0")} SRM";

    public override IngredientData ToData() => base.ToData() with
    {
        YieldPercent = YieldPercent,
        ColorLovibond = ColorLovibond,
        IsMashed = IsMashed,
    };

    public override Malt Clone() => (Malt)MemberwiseClone();

    protected override IEnumerable<string> ValidateCharacteristics()
    {
        if (!YieldRange.Contains(YieldPercent))
        {
            yield return $"{Label}: вихід екстракту має бути від 0 до 100 %.";
        }

        if (!(ColorLovibond >= 0))
        {
            yield return $"{Label}: колір не може бути від'ємним.";
        }
    }
}
