using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BrewLog.UI.Converters;

/// <summary>
/// Знаходить ресурс теми за префіксом із параметра й кодом зі значення: «Chip.» + «Brewing» → стиль Chip.Brewing.
/// Завдяки цьому вигляд етапу береться з теми за його кодом, без перебору станів у коді.
/// </summary>
public sealed class ResourceByKeyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Application.Current.TryFindResource($"{parameter}{value}") ?? DependencyProperty.UnsetValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
