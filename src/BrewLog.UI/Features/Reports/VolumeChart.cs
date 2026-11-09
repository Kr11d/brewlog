using System.Windows;
using BrewLog.UI.Formatting;
using ScottPlot;
using ScottPlot.TickGenerators;
using PlotColor = ScottPlot.Color;
using ThemeColor = System.Windows.Media.Color;

namespace BrewLog.UI.Features.Reports;

/// <summary>Вигляд графіка обсягу: той самий для сторінки й для зображення в PDF.</summary>
public static class VolumeChart
{
    private const double BarWidth = 0.7;
    private const double HeadroomFactor = 1.15;

    /// <summary>Шрифт із кирилицею, кольори палітри, без сітки.</summary>
    public static void Configure(Plot plot)
    {
        plot.Font.Set("Segoe UI");
        plot.FigureBackground.Color = Palette("Surface.Color");
        plot.DataBackground.Color = Palette("Surface.Color");
        plot.Axes.Color(Palette("Text.Secondary.Color"));
        plot.HideGrid();
        plot.Axes.Left.TickGenerator = new NumericAutomatic { LabelFormatter = DisplayFormat.Number };
    }

    /// <summary>Стовпчики Amber, вибраний місяць — Accent, підписи місяців — скорочено.</summary>
    public static void Draw(Plot plot, IReadOnlyList<MonthBar> bars)
    {
        plot.Clear();
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
        plot.Axes.SetLimitsY(0, Math.Max(bars.Max(bar => bar.Liters) * HeadroomFactor, 1));
    }

    /// <summary>Малює графік у пам'яті, без елемента на екрані: для вставки в PDF.</summary>
    public static byte[] RenderPng(IReadOnlyList<MonthBar> bars, int width, int height)
    {
        using var plot = new Plot();
        Configure(plot);
        Draw(plot, bars);
        return plot.GetImageBytes(width, height, ImageFormat.Png);
    }

    private static PlotColor Palette(string key)
    {
        var color = (ThemeColor)Application.Current.FindResource(key);
        return new PlotColor(color.R, color.G, color.B, color.A);
    }
}
