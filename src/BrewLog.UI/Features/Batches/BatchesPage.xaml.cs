using System.Windows.Controls;
using System.Windows.Input;

namespace BrewLog.UI.Features.Batches;

/// <summary>Розмітка сторінки партій; логіка — у BatchesViewModel.</summary>
public partial class BatchesPage : UserControl
{
    public BatchesPage()
    {
        InitializeComponent();
    }

    /// <summary>Рядок DataGrid не має команди кліку. Кнопка в рядку сама обробляє клік, тож сюди він не доходить.</summary>
    private void OnRowClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow { Item: BatchItemViewModel item } && DataContext is BatchesViewModel viewModel)
        {
            viewModel.OpenCommand.Execute(item);
        }
    }
}
