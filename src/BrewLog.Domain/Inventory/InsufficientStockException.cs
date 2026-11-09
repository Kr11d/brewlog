using System.Globalization;
using BrewLog.Domain.Common;

namespace BrewLog.Domain.Inventory;

/// <summary>Сировини бракує для списання. Повідомлення перелічує нестачі українською, а Shortages дає їх окремими записами.</summary>
public sealed class InsufficientStockException : DomainException
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("uk-UA");

    public InsufficientStockException(IReadOnlyList<Shortage> shortages) : base(BuildMessage(shortages))
    {
        Shortages = shortages;
    }

    public IReadOnlyList<Shortage> Shortages { get; }

    private static string BuildMessage(IEnumerable<Shortage> shortages) =>
        "Бракує сировини на складі:" + Environment.NewLine + string.Join(
            Environment.NewLine,
            shortages.Select(item => $"{item.Name}: потрібно {Format(item.Required)} {item.Unit}, доступно {Format(item.Available)} {item.Unit}"));

    private static string Format(double value) => value.ToString("0.###", DisplayCulture);
}
