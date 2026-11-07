using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using BrewLog.Domain.Recipes;

namespace BrewLog.UI.Converters;

/// <summary>Колір пива в SRM → пензель для кольорової плашки; сам колір рахує SrmColor у Domain.</summary>
public sealed class SrmToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var (r, g, b) = SrmColor.ToRgb(value is double srm ? srm : 0);
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
