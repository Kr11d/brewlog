using System.Windows;

namespace BrewLog.UI.Features.Bottling;

/// <summary>Вікно розливу партії. Відкривається лише через WindowService.</summary>
public partial class BottlingWindow : Window
{
    public BottlingWindow(BottlingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
