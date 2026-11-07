using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace BrewLog.UI.Controls;

/// <summary>
/// Передає колесо миші зовнішньому списку. Таблиця має власний ScrollViewer, який поглинає подію, навіть коли прокручувати нічого,
/// тож без цього список із таблицями не рухався б, коли курсор стоїть над таблицею.
/// </summary>
public static class WheelForwarding
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(WheelForwarding), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (element is not UIElement target)
        {
            return;
        }

        target.PreviewMouseWheel -= Forward;
        if ((bool)args.NewValue)
        {
            target.PreviewMouseWheel += Forward;
        }
    }

    private static void Forward(object sender, MouseWheelEventArgs args)
    {
        if (VisualTreeHelper.GetParent((DependencyObject)sender) is UIElement parent)
        {
            args.Handled = true;
            parent.RaiseEvent(new MouseWheelEventArgs(args.MouseDevice, args.Timestamp, args.Delta)
            {
                RoutedEvent = UIElement.MouseWheelEvent,
                Source = sender,
            });
        }
    }
}
