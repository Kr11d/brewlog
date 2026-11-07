using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BrewLog.UI.Controls;

/// <summary>
/// Гаряча клавіша Ctrl+F: переводить фокус у поле пошуку активного вікна, зокрема сторінки всередині головного.
/// Поле позначає стиль SearchBox, тому екрану більше нічого налаштовувати не треба.
/// </summary>
public static class SearchShortcut
{
    public static readonly DependencyProperty IsTargetProperty = DependencyProperty.RegisterAttached(
        "IsTarget", typeof(bool), typeof(SearchShortcut), new PropertyMetadata(false));

    private static readonly RoutedCommand FocusSearch = new(nameof(FocusSearch), typeof(SearchShortcut));

    static SearchShortcut()
    {
        CommandManager.RegisterClassInputBinding(typeof(Window), new KeyBinding(FocusSearch, Key.F, ModifierKeys.Control));
        CommandManager.RegisterClassCommandBinding(typeof(Window), new CommandBinding(FocusSearch, (sender, _) => FocusTarget((Window)sender)));
    }

    public static bool GetIsTarget(DependencyObject element) => (bool)element.GetValue(IsTargetProperty);

    public static void SetIsTarget(DependencyObject element, bool value) => element.SetValue(IsTargetProperty, value);

    /// <summary>Фокус у перше видиме поле пошуку; текст виділяється, щоб новий запит одразу замінив старий.</summary>
    public static void FocusTarget(DependencyObject root)
    {
        if (FindTarget(root) is { } field)
        {
            field.Focus();
            field.SelectAll();
        }
    }

    private static TextBox? FindTarget(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is TextBox { IsVisible: true } field && GetIsTarget(field))
            {
                return field;
            }

            if (FindTarget(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }
}
