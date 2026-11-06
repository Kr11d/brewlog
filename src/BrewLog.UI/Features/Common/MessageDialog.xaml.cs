using System.Windows;

namespace BrewLog.UI.Features.Common;

/// <summary>Стилізоване повідомлення замість системного MessageBox. Відкривається лише через DialogService.</summary>
public partial class MessageDialog : Window
{
    public MessageDialog(MessageDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.CloseRequested += result => DialogResult = result;
    }
}
