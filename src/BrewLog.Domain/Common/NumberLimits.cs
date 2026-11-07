namespace BrewLog.Domain.Common;

/// <summary>
/// Межі чисел, які надходять ззовні: з полів форми чи з файлу. Показники й собівартість рахуються одразу,
/// а нескінченність, надто велике чи надто мале число переповнило б decimal і зламало б сторінку з цим рецептом.
/// </summary>
public static class NumberLimits
{
    private const double MinMagnitude = 1e-6;
    private const double MaxMagnitude = 1e9;

    public static bool IsAllowed(double value) => value == 0 || Math.Abs(value) is >= MinMagnitude and <= MaxMagnitude;
}
