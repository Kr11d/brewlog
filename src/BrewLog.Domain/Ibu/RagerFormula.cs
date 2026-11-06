using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Ibu;

/// <summary>Формула Рейджера: дає вищу гіркоту за Тінсета й знижує її лише для густин понад 1,050.</summary>
public sealed class RagerFormula : IbuFormula
{
    public override string Code => "Rager";

    public override string DisplayName => "Rager";

    /// <summary>
    /// U = (18,11 + 13,86 · tanh((t − 31,32) / 18,27)) / 100;
    /// GA = G > 1,050 ? (G − 1,050) / 0,2 : 0;
    /// IBU = грами · U · α · 1000 / (л · (1 + GA)), де α — частка, а не відсотки.
    /// </summary>
    public override double Calculate(Hop hop, double boilGravity, double volumeL)
    {
        var utilization = (18.11 + 13.86 * Math.Tanh((hop.TimeMin - 31.32) / 18.27)) / 100;
        var gravityAdjustment = boilGravity > 1.050 ? (boilGravity - 1.050) / 0.2 : 0;

        return hop.Amount * utilization * (hop.AlphaPercent / 100) * 1000 / (volumeL * (1 + gravityAdjustment));
    }
}
