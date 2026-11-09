using BrewLog.Domain.Common;

namespace BrewLog.Domain.Recipes;

/// <summary>Додаток (цукор, декстроза): дає екстракт без ефективності й не впливає на колір.</summary>
public sealed class Adjunct : Ingredient
{
    private static readonly ValueRange YieldRange = new(0, 100);

    /// <summary>Вихід екстракту, %.</summary>
    public double YieldPercent { get; set; }

    public override IngredientKind Kind => IngredientKind.Adjunct;

    public override string Unit => "кг";

    public override string KindDisplayName => "Додатки";

    /// <summary>Додає екстракт: цукор розчиняється повністю, тому ефективність затирання не діє.</summary>
    public override void ApplyTo(RecipeStats stats) => stats.AddExtract(this, YieldPercent, usesEfficiency: false);

    public override IngredientData ToData() => base.ToData() with { YieldPercent = YieldPercent };

    public override Adjunct Clone() => (Adjunct)MemberwiseClone();

    protected override IEnumerable<string> ValidateCharacteristics()
    {
        if (!YieldRange.Contains(YieldPercent))
        {
            yield return $"{Label}: вихід екстракту має бути від 0 до 100 %.";
        }
    }
}
