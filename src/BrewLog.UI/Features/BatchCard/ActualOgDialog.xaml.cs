using System.Windows;

namespace BrewLog.UI.Features.BatchCard;

/// <summary>Діалог фактичної OG. Відкривається лише через WindowService.</summary>
public partial class ActualOgDialog : Window
{
    public ActualOgDialog(ActualOgViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += result => DialogResult = result;
    }
}
