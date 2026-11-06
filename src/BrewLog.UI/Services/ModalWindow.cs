using System.Windows;

namespace BrewLog.UI.Services;

/// <summary>Показує модальне вікно поверх активного вікна застосунку.</summary>
public static class ModalWindow
{
    public static bool? Show(Window window)
    {
        var owner = FindOwner();
        window.Owner = owner;
        window.WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
        return window.ShowDialog();
    }

    /// <summary>Власником може бути лише вже показане вікно, інакше WPF кидає виняток.</summary>
    private static Window? FindOwner()
    {
        var visibleWindows = Application.Current.Windows.OfType<Window>().Where(window => window.IsVisible).ToList();
        return visibleWindows.FirstOrDefault(window => window.IsActive) ?? visibleWindows.FirstOrDefault();
    }
}
