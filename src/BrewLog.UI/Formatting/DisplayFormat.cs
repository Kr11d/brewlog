using System.Globalization;
using BrewLog.Domain.Common;

namespace BrewLog.UI.Formatting;

/// <summary>
/// Формати чисел інтерфейсу в одному місці (культура uk-UA): густина — 3 знаки, ABV — 1 знак із «%»,
/// IBU та колір — цілі, гроші — «0,00 грн». ViewModel готує ними рядки для розмітки.
/// </summary>
public static class DisplayFormat
{
    private const NumberStyles InputStyles =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

    /// <summary>Значення для поля вводу, без зайвих нулів: «12,5», «60».</summary>
    public static string Number(double value) => value.ToString("0.###", Culture);

    /// <summary>Читає число, яке друкує користувач: підходять і кома, і крапка, а значення має бути в межах NumberLimits.</summary>
    public static bool TryParseNumber(string? text, out double value)
    {
        var isNumber = double.TryParse(text?.Replace('.', ','), InputStyles, Culture, out value);
        return isNumber && NumberLimits.IsAllowed(value);
    }

    /// <summary>Густина: «1,048».</summary>
    public static string Gravity(double value) => value.ToString("0.000", Culture);

    /// <summary>Міцність: «4,9 %».</summary>
    public static string Abv(double value) => $"{value.ToString("0.0", Culture)} %";

    /// <summary>Ціле значення, наприклад IBU, SRM чи EBC.</summary>
    public static string Integer(double value) => value.ToString("0", Culture);

    /// <summary>Сума без одиниці: «1 234,50».</summary>
    public static string MoneyValue(decimal value) => value.ToString("#,0.00", Culture);

    /// <summary>Сума: «1 234,50 грн».</summary>
    public static string Money(decimal value) => $"{MoneyValue(value)} грн";

    /// <summary>Кількість інгредієнта з одиницею: «12,2 кг», «60 г», «3 пак.».</summary>
    public static string Amount(double value, string unit) => $"{value.ToString("0.###", Culture)} {unit}";

    /// <summary>Число з узгодженим іменником: Count(1, "рецепт", "рецепти", "рецептів") → «1 рецепт», 3 → «3 рецепти», 5 → «5 рецептів».</summary>
    public static string Count(int count, string one, string few, string many)
    {
        var lastTwo = Math.Abs(count) % 100;
        var last = lastTwo % 10;
        var noun = lastTwo is >= 11 and <= 14 ? many : last switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many,
        };
        return $"{count} {noun}";
    }
}
