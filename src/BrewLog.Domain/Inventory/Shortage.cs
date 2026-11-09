using System.Globalization;

namespace BrewLog.Domain.Inventory;

/// <summary>Нестача однієї позиції: скільки потрібно і скільки є придатного, в одиницях складу.</summary>
public sealed record Shortage(string Name, string Unit, double Required, double Available)
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("uk-UA");

    /// <summary>Перелік нестач для користувача: і для винятку під час списання, і для причини відмови в переході.</summary>
    public static string DescribeAll(IEnumerable<Shortage> shortages) =>
        "Бракує сировини на складі:" + Environment.NewLine + string.Join(
            Environment.NewLine,
            shortages.Select(item => $"{item.Name}: потрібно {Format(item.Required)} {item.Unit}, доступно {Format(item.Available)} {item.Unit}"));

    private static string Format(double value) => value.ToString("0.###", DisplayCulture);
}
