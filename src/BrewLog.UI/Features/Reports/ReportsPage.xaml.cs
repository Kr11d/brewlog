using System.Windows;
using System.Windows.Controls;

namespace BrewLog.UI.Features.Reports;

/// <summary>Сторінка «Звіти». Тут лише оновлення графіка з даних ViewModel.</summary>
public partial class ReportsPage : UserControl
{
    private ReportsViewModel? _viewModel;

    public ReportsPage()
    {
        InitializeComponent();
        VolumePlot.UserInputProcessor.Disable();
        VolumeChart.Configure(VolumePlot.Plot);
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.ChartChanged -= DrawChart;
        }

        _viewModel = e.NewValue as ReportsViewModel;
        if (_viewModel is not null)
        {
            _viewModel.ChartChanged += DrawChart;
            DrawChart();
        }
    }

    private void DrawChart()
    {
        VolumeChart.Draw(VolumePlot.Plot, _viewModel!.Bars);
        VolumePlot.Refresh();
    }
}
