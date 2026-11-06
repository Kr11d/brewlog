using BrewLog.Domain.Ibu;

namespace BrewLog.Domain.Recipes;

/// <summary>
/// Накопичувач показників рецепта. Інгредієнти додають сюди свій внесок через ApplyTo, а Finish рахує результат.
/// Запам'ятовує внесок кожного рядка, щоб редактор міг показати його окремо.
/// За обсягу 0 внески дорівнюють нулю: показники лишаються скінченними навіть для незаповненої форми.
/// </summary>
public sealed class RecipeStats
{
    /// <summary>Константа екстракту сахарози, пункта·л/кг: 46,21 PPG × 8,345.</summary>
    private const double SucroseExtract = 385.6;

    private const double PoundsPerKilogram = 2.20462;
    private const double GallonsPerLiter = 0.264172;
    private const double DefaultAttenuationPercent = 75;

    private readonly Dictionary<Ingredient, double> _pointsByLine = [];
    private readonly Dictionary<Ingredient, double> _mcuByLine = [];
    private readonly Dictionary<Ingredient, double> _ibuByLine = [];
    private readonly List<Hop> _boilHops = [];
    private double? _yeastAttenuationPercent;

    public RecipeStats(double volumeL, double efficiencyPercent)
    {
        VolumeL = volumeL;
        EfficiencyPercent = efficiencyPercent;
    }

    public double VolumeL { get; }

    public double EfficiencyPercent { get; }

    /// <summary>Сума пунктів екстракту всіх рядків.</summary>
    public double ExtractPoints { get; private set; }

    /// <summary>Сума MCU всіх рядків.</summary>
    public double Mcu { get; private set; }

    /// <summary>Максимальне зброджування серед дріжджів; без дріжджів 75 %.</summary>
    public double AttenuationPercent => _yeastAttenuationPercent ?? DefaultAttenuationPercent;

    /// <summary>Нижня межа температури дріжджів із максимальним зброджуванням; без дріжджів порожньо.</summary>
    public double? TempMinC { get; private set; }

    /// <summary>Верхня межа температури дріжджів із максимальним зброджуванням; без дріжджів порожньо.</summary>
    public double? TempMaxC { get; private set; }

    public double Og { get; private set; } = 1;

    public double Fg { get; private set; } = 1;

    public double Abv { get; private set; }

    public double Ibu { get; private set; }

    public double Srm { get; private set; }

    public double Ebc { get; private set; }

    /// <summary>Додає екстракт рядка: пункти = кг × Yield/100 × 385,6 × (ефективність/100, якщо діє) / обсяг, л.</summary>
    public void AddExtract(Ingredient line, double yieldPercent, bool usesEfficiency)
    {
        if (VolumeL <= 0)
        {
            return;
        }

        var efficiency = usesEfficiency ? EfficiencyPercent / 100 : 1;
        var points = line.Amount * (yieldPercent / 100) * SucroseExtract * efficiency / VolumeL;

        _pointsByLine[line] = points;
        ExtractPoints += points;
    }

    /// <summary>Додає колір рядка: MCU = кг · 2,20462 · °L / (л · 0,264172).</summary>
    public void AddColor(Ingredient line, double colorLovibond)
    {
        if (VolumeL <= 0)
        {
            return;
        }

        var mcu = line.Amount * PoundsPerKilogram * colorLovibond / (VolumeL * GallonsPerLiter);

        _mcuByLine[line] = mcu;
        Mcu += mcu;
    }

    /// <summary>Реєструє варильне внесення: його IBU рахує Finish, коли вже відома OG.</summary>
    public void AddBoilHop(Hop hop) => _boilHops.Add(hop);

    /// <summary>Запам'ятовує дріжджі з найбільшим зброджуванням: вони визначають FG і температуру бродіння.</summary>
    public void AddYeast(double attenuationPercent, double tempMinC, double tempMaxC)
    {
        if (_yeastAttenuationPercent is null || attenuationPercent > _yeastAttenuationPercent)
        {
            _yeastAttenuationPercent = attenuationPercent;
            TempMinC = tempMinC;
            TempMaxC = tempMaxC;
        }
    }

    /// <summary>
    /// Рахує результат за накопиченими внесками:
    /// OG = 1 + пункти / 1000; FG = 1 + (OG − 1) · (1 − A); ABV = (OG − FG) · 131,25;
    /// SRM = 1,4922 · MCU^0,6859 (Morey); EBC = SRM · 1,97; IBU — сума за обраною формулою з G = OG.
    /// </summary>
    public void Finish(IbuFormula formula)
    {
        Og = 1 + ExtractPoints / 1000;
        Fg = 1 + (Og - 1) * (1 - AttenuationPercent / 100);
        Abv = (Og - Fg) * 131.25;

        // Від'ємна кількість можлива, поки користувач друкує в редакторі, а степінь від'ємного числа дала б NaN.
        Srm = 1.4922 * Math.Pow(Math.Max(Mcu, 0), 0.6859);
        Ebc = Srm * 1.97;

        _ibuByLine.Clear();
        if (VolumeL > 0)
        {
            foreach (var hop in _boilHops)
            {
                _ibuByLine[hop] = formula.Calculate(hop, Og, VolumeL);
            }
        }

        Ibu = _ibuByLine.Values.Sum();
    }

    /// <summary>Пункти екстракту, які дав цей рядок.</summary>
    public double ExtractPointsOf(Ingredient line) => _pointsByLine.GetValueOrDefault(line);

    /// <summary>Частка рядка в SRM рецепта, пропорційна його MCU: сума часток усіх рядків дорівнює SRM.</summary>
    public double SrmOf(Ingredient line) => Mcu > 0 ? Srm * _mcuByLine.GetValueOrDefault(line) / Mcu : 0;

    /// <summary>IBU, які дав цей рядок.</summary>
    public double IbuOf(Ingredient line) => _ibuByLine.GetValueOrDefault(line);
}
