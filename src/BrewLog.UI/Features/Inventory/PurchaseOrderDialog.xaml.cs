using System.Windows;
using BrewLog.Domain.Inventory;

namespace BrewLog.UI.Features.Inventory;

/// <summary>Діалог замовлення постачальнику. Відкривається лише через WindowService.</summary>
public partial class PurchaseOrderDialog : Window
{
    private readonly PurchaseOrderViewModel _viewModel;

    public PurchaseOrderDialog(PurchaseOrderViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        viewModel.CloseRequested += result => DialogResult = result;
    }

    public PurchaseOrder? Order => _viewModel.Order;
}
