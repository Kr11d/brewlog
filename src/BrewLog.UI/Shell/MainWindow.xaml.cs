using System.Windows;

namespace BrewLog.UI.Shell;

/// <summary>Головне вікно: бічне меню й область поточної сторінки.</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
