using System.Windows;
using BrewLog.UI.Formatting;
using ScottPlot;
using ScottPlot.TickGenerators;
using ScottPlot.TickGenerators.TimeUnits;
using ScottPlot.WPF;
using PlotColor = ScottPlot.Color;
using ThemeColor = System.Windows.Media.Color;

namespace BrewLog.UI.Features.BatchCard;

/// <summary>Вікно «Картка партії». Тут лише малювання графіків із даних ViewModel. Відкривається через WindowService.</summary>
public partial class BatchCardWindow : Window
{
    private const float LineWidth = 2;
    private const float MarkerSize = 7;
    private const float OutOfRangeMarkerSize = 10;
    private const double BandOpacity = 0.15;
    private const float TargetLineWidth = 1.5f;
    private const float LabelFontSize = 12;
    private const int MaxDayLabels = 10;

    private readonly BatchCardViewModel _viewModel;

    public BatchCardWindow(BatchCardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;

        Configure(GravityPlot, DisplayFormat.Gravity);
        Configure(TemperaturePlot, DisplayFormat.Number);
        viewModel.ChartsChanged += DrawCharts;
        DrawCharts();
    }

    /// <summary>Спільний вигляд графіків: шрифт із кирилицею, кольори палітри, дати по осі X, без сітки й масштабування мишею.</summary>
    private void Configure(WpfPlot chart, Func<double, string> formatY)
    {
        chart.UserInputProcessor.Disable();

        var plot = chart.Plot;
        plot.Font.Set("Segoe UI");
        plot.FigureBackground.Color = Palette("Surface.Color");
        plot.DataBackground.Color = Palette("Surface.Color");
        plot.Axes.Color(Palette("Text.Secondary.Color"));
        plot.HideGrid();

        plot.Axes.DateTimeTicksBottom();
        plot.Axes.Left.TickGenerator = new NumericAutomatic { LabelFormatter = value => formatY(value) };
    }

    private void DrawCharts()
    {
        DrawGravity(_viewModel.Details);
        DrawTemperature(_viewModel.Details);
    }

    private void DrawGravity(BatchDetailsViewModel details)
    {
        var plot = GravityPlot.Plot;
        plot.Clear();

        if (details.HasGravity)
        {
            plot.Add.HorizontalLine(details.PlannedFg, TargetLineWidth, Palette("Text.Secondary.Color"), LinePattern.Dashed);

            // Підпис — на самій лінії зліва: густина падає згори, тож там він нічого не перекриває.
            var label = plot.Add.Text(details.PlannedFgLabel, details.GravityPoints[0].At.ToOADate(), details.PlannedFg);
            label.Alignment = Alignment.LowerLeft;
            label.LabelFontColor = Palette("Text.Secondary.Color");
            label.LabelFontSize = LabelFontSize;

            AddLine(plot, details.GravityPoints, Palette("Amber.Color"));
            ShowDays(plot, details.GravityPoints);
        }

        plot.Axes.AutoScale();
        GravityPlot.Refresh();
    }

    private void DrawTemperature(BatchDetailsViewModel details)
    {
        var plot = TemperaturePlot.Plot;
        plot.Clear();

        if (details.TempMinC is { } min && details.TempMaxC is { } max)
        {
            var band = plot.Add.VerticalSpan(min, max);
            band.FillColor = Palette("Info.Color").WithAlpha(BandOpacity);
            band.LineStyle.Width = 0;
        }

        AddLine(plot, details.TemperaturePoints, Palette("Info.Color"));
        if (details.HasTemperature)
        {
            ShowDays(plot, details.TemperaturePoints);
        }

        var outOfRange = details.TemperaturePoints.Where(point => point.IsOutOfRange).ToList();
        if (outOfRange.Count > 0)
        {
            plot.Add.Markers(Dates(outOfRange), Values(outOfRange), size: OutOfRangeMarkerSize, color: Palette("Danger.Color"));
        }

        plot.Axes.AutoScale();
        TemperaturePlot.Refresh();
    }

    private static void AddLine(Plot plot, IReadOnlyList<ChartPoint> points, PlotColor color)
    {
        if (points.Count > 0)
        {
            var line = plot.Add.Scatter(Dates(points), Values(points), color);
            line.LineWidth = LineWidth;
            line.MarkerSize = MarkerSize;
        }
    }

    /// <summary>Мітки по осі X — лише цілі дні: автоматичний крок на коротких партіях повторював би ту саму дату.</summary>
    private static void ShowDays(Plot plot, IReadOnlyList<ChartPoint> points)
    {
        var days = (points[^1].At.Date - points[0].At.Date).Days;
        plot.Axes.Bottom.TickGenerator = new DateTimeFixedInterval(new Day(), Math.Max(1, (days + MaxDayLabels - 1) / MaxDayLabels))
        {
            LabelFormatter = DisplayFormat.DayAndMonth,
        };
    }

    private static double[] Dates(IEnumerable<ChartPoint> points) => points.Select(point => point.At.ToOADate()).ToArray();

    private static double[] Values(IEnumerable<ChartPoint> points) => points.Select(point => point.Value).ToArray();

    /// <summary>Колір із палітри теми в форматі ScottPlot: кольори графіків не дублюються в коді.</summary>
    private PlotColor Palette(string key)
    {
        var color = (ThemeColor)FindResource(key);
        return new PlotColor(color.R, color.G, color.B, color.A);
    }
}
