using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace BrewLog.UI.Controls;

/// <summary>
/// Фарбує системний заголовок вікна в кольори теми, щоб він зливався з градієнтним фоном.
/// Рамка й кнопки вікна лишаються рідними. Працює на Windows 11; на старіших системах заголовок просто стандартний.
/// </summary>
public static class WindowCaption
{
    private const int CaptionColorAttribute = 35;
    private const int TextColorAttribute = 36;

    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.RegisterAttached(
        "Background", typeof(Color), typeof(WindowCaption), new PropertyMetadata(default(Color), OnColorChanged));

    public static readonly DependencyProperty ForegroundProperty = DependencyProperty.RegisterAttached(
        "Foreground", typeof(Color), typeof(WindowCaption), new PropertyMetadata(default(Color), OnColorChanged));

    public static Color GetBackground(DependencyObject element) => (Color)element.GetValue(BackgroundProperty);

    public static void SetBackground(DependencyObject element, Color value) => element.SetValue(BackgroundProperty, value);

    public static Color GetForeground(DependencyObject element) => (Color)element.GetValue(ForegroundProperty);

    public static void SetForeground(DependencyObject element, Color value) => element.SetValue(ForegroundProperty, value);

    private static void OnColorChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is Window window)
        {
            window.SourceInitialized -= ApplyColors;
            window.SourceInitialized += ApplyColors;
        }
    }

    private static void ApplyColors(object? sender, EventArgs e)
    {
        var window = (Window)sender!;
        var handle = new WindowInteropHelper(window).Handle;
        SetColor(handle, CaptionColorAttribute, GetBackground(window));
        SetColor(handle, TextColorAttribute, GetForeground(window));
    }

    private static void SetColor(IntPtr handle, int attribute, Color color)
    {
        var colorRef = color.R | (color.G << 8) | (color.B << 16);
        DwmSetWindowAttribute(handle, attribute, ref colorRef, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr handle, int attribute, ref int value, int size);
}
