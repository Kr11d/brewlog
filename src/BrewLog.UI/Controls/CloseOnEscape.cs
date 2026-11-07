using System.Windows;
using System.Windows.Input;

namespace BrewLog.UI.Controls;

/// <summary>
/// Esc закриває вікно. Увімкнено в стилі DialogWindow, тож кожен діалог отримує це сам.
/// Відкритий випадний список спершу закривається сам: він позначає подію обробленою, і вікно лишається.
/// </summary>
public static class CloseOnEscape
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(CloseOnEscape), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not Window window)
        {
            return;
        }

        window.KeyDown -= Close;
        if ((bool)args.NewValue)
        {
            window.KeyDown += Close;
        }
    }

    private static void Close(object sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape)
        {
            args.Handled = true;
            ((Window)sender).Close();
        }
    }
}
