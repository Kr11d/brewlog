using System.Windows;
using System.Windows.Controls;

namespace BrewLog.UI.Controls;

/// <summary>
/// Показує, чи порожній PasswordBox. Сам PasswordBox цього не повідомляє: його Password не є властивістю залежностей,
/// тому шаблон теми без цього помічника не знав би, коли ховати плейсхолдер.
/// </summary>
public static class PasswordAssist
{
    public static readonly DependencyProperty TrackEmptyProperty = DependencyProperty.RegisterAttached(
        "TrackEmpty", typeof(bool), typeof(PasswordAssist), new PropertyMetadata(false, OnTrackEmptyChanged));

    private static readonly DependencyPropertyKey IsEmptyPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "IsEmpty", typeof(bool), typeof(PasswordAssist), new PropertyMetadata(true));

    public static readonly DependencyProperty IsEmptyProperty = IsEmptyPropertyKey.DependencyProperty;

    public static bool GetTrackEmpty(DependencyObject element) => (bool)element.GetValue(TrackEmptyProperty);

    public static void SetTrackEmpty(DependencyObject element, bool value) => element.SetValue(TrackEmptyProperty, value);

    public static bool GetIsEmpty(DependencyObject element) => (bool)element.GetValue(IsEmptyProperty);

    private static void OnTrackEmptyChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is PasswordBox passwordBox)
        {
            passwordBox.PasswordChanged -= OnPasswordChanged;
            if ((bool)e.NewValue)
            {
                passwordBox.PasswordChanged += OnPasswordChanged;
            }
        }
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        var passwordBox = (PasswordBox)sender;
        passwordBox.SetValue(IsEmptyPropertyKey, passwordBox.Password.Length == 0);
    }
}
