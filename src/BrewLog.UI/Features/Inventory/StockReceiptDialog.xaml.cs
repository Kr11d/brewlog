using System.Windows;

namespace BrewLog.UI.Features.Inventory;

/// <summary>Діалог приходу сировини. Відкривається лише через WindowService.</summary>
public partial class StockReceiptDialog : Window
{
    public StockReceiptDialog(StockReceiptViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += result => DialogResult = result;
    }
}
