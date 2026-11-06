using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Ibu;

/// <summary>Формула Тінсета: типова для рецептів.</summary>
public sealed class TinsethFormula : IbuFormula
{
    public override string Code => "Tinseth";

    public override string DisplayName => "Tinseth";

    /// <summary>IBU = 1,65 · 0,000125^(G − 1) · (1 − e^(−0,04·t)) / 4,15 · (α · грами · 1000 / л), де α — частка, а не відсотки.</summary>
    public override double Calculate(Hop hop, double boilGravity, double volumeL)
    {
        var gravityFactor = 1.65 * Math.Pow(0.000125, boilGravity - 1);
        var timeFactor = (1 - Math.Exp(-0.04 * hop.TimeMin)) / 4.15;
        var alphaAcidsMgPerLiter = hop.AlphaPercent / 100 * hop.Amount * 1000 / volumeL;

        return gravityFactor * timeFactor * alphaAcidsMgPerLiter;
    }
}
