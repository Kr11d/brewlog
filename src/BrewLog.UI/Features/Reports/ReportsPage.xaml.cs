using System.Windows;
using System.Windows.Controls;
using BrewLog.UI.Formatting;
using ScottPlot;
using ScottPlot.TickGenerators;
using PlotColor = ScottPlot.Color;
using ThemeColor = System.Windows.Media.Color;

namespace BrewLog.UI.Features.Reports;

/// <summary>Сторінка «Звіти». Тут лише малювання графіка з даних ViewModel.</summary>
public partial class ReportsPage : UserControl
{
    private const double BarWidth = 0.7;

    private ReportsViewModel? _viewModel;

    public ReportsPage()
    {
        InitializeComponent();
        ConfigureChart();
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

    /// <summary>Спільний вигляд: шрифт із кирилицею, кольори палітри, без сітки й масштабування мишею.</summary>
    private void ConfigureChart()
    {
        VolumePlot.UserInputProcessor.Disable();

        var plot = VolumePlot.Plot;
        plot.Font.Set("Segoe UI");
        plot.FigureBackground.Color = Palette("Surface.Color");
        plot.DataBackground.Color = Palette("Surface.Color");
        plot.Axes.Color(Palette("Text.Secondary.Color"));
        plot.HideGrid();
        plot.Axes.Left.TickGenerator = new NumericAutomatic { LabelFormatter = DisplayFormat.Number };
    }

    private void DrawChart()
    {
        if (_viewModel is null)
        {
            return;
        }

        var plot = VolumePlot.Plot;
        plot.Clear();

        var bars = _viewModel.Bars;
        plot.Add.Bars(bars.Select((bar, index) => new Bar
        {
            Position = index,
            Value = bar.Liters,
            Size = BarWidth,
            FillColor = Palette(bar.IsSelected ? "Accent.Color" : "Amber.Color"),
            LineWidth = 0,
        }).ToArray());

        plot.Axes.Bottom.TickGenerator = new NumericManual(
            Enumerable.Range(0, bars.Count).Select(index => (double)index).ToArray(),
            bars.Select(bar => bar.Label).ToArray());
        plot.Axes.AutoScale();
        plot.Axes.SetLimitsY(0, Math.Max(bars.Max(bar => bar.Liters) * 1.15, 1));
        VolumePlot.Refresh();
    }

    /// <summary>Колір із палітри теми у форматі ScottPlot.</summary>
    private PlotColor Palette(string key)
    {
        var color = (ThemeColor)FindResource(key);
        return new PlotColor(color.R, color.G, color.B, color.A);
    }
}
