using BrewLog.Domain.Common;

namespace BrewLog.Domain.Recipes;

/// <summary>Дріжджі: задають ступінь зброджування й температурний діапазон бродіння.</summary>
public sealed class Yeast : Ingredient
{
    private static readonly ValueRange AttenuationRange = new(50, 100);

    /// <summary>Ступінь зброджування, %.</summary>
    public double AttenuationPercent { get; set; }

    public double TempMinC { get; set; }

    public double TempMaxC { get; set; }

    public override IngredientKind Kind => IngredientKind.Yeast;

    public override string Unit => "пак.";

    /// <summary>Передає зброджування й температури: екстракту, кольору й гіркоти дріжджі не додають.</summary>
    public override void ApplyTo(RecipeStats stats) => stats.AddYeast(AttenuationPercent, TempMinC, TempMaxC);

    /// <summary>Наприклад, «зброджування 77 %».</summary>
    public override string DescribeContribution(RecipeStats final) =>
        $"зброджування {FormatNumber(AttenuationPercent, "0.#")} %";

    public override IngredientData ToData() => base.ToData() with
    {
        AttenuationPercent = AttenuationPercent,
        TempMinC = TempMinC,
        TempMaxC = TempMaxC,
    };

    public override Yeast Clone() => (Yeast)MemberwiseClone();

    protected override IEnumerable<string> ValidateCharacteristics()
    {
        if (!AttenuationRange.Contains(AttenuationPercent))
        {
            yield return $"{Label}: зброджування має бути від 50 до 100 %.";
        }

        if (!(TempMinC < TempMaxC))
        {
            yield return $"{Label}: мінімальна температура має бути нижчою за максимальну.";
        }
    }
}
