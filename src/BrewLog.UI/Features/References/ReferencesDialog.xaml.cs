using System.Windows;

namespace BrewLog.UI.Features.References;

/// <summary>Діалог «Довідники». Відкривається лише через WindowService.</summary>
public partial class ReferencesDialog : Window
{
    public ReferencesDialog(ReferencesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Хрестик заголовка не має мовчки втратити незбережені зміни.
        Closing += (_, e) => e.Cancel = !viewModel.ConfirmClose();
    }
}
