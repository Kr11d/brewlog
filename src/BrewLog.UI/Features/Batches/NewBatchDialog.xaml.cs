using System.Windows;

namespace BrewLog.UI.Features.Batches;

/// <summary>Діалог планування партії. Відкривається лише через WindowService.</summary>
public partial class NewBatchDialog : Window
{
    public NewBatchDialog(NewBatchViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += result => DialogResult = result;
    }
}
