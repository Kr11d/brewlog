using System.Globalization;
using System.Windows.Data;

namespace BrewLog.UI.Converters;

/// <summary>Перетворює повне ім'я на ініціали для кола профілю: «Андрій Солодовник» → «АС».</summary>
public sealed class InitialsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var words = (value as string ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(words.Take(2).Select(word => char.ToUpper(word[0], culture)));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
