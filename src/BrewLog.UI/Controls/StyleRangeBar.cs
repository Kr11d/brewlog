using System.Windows;
using System.Windows.Media;

namespace BrewLog.UI.Controls;

/// <summary>
/// Шкала відповідності стилю: уся смуга — діапазон стилю, маркер — значення рецепта.
/// Значення поза діапазоном притискається до краю, тож вихід за межі показує колір із теми (стиль StyleRangeBar).
/// </summary>
public sealed class StyleRangeBar : FrameworkElement
{
    /// <summary>Положення маркера: 0 — нижня межа стилю, 1 — верхня.</summary>
    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
        nameof(Position), typeof(double), typeof(StyleRangeBar), new FrameworkPropertyMetadata(0.5, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(StyleRangeBar), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MarkerBrushProperty = DependencyProperty.Register(
        nameof(MarkerBrush), typeof(Brush), typeof(StyleRangeBar), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public double Position
    {
        get => (double)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    public Brush? TrackBrush
    {
        get => (Brush?)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public Brush? MarkerBrush
    {
        get => (Brush?)GetValue(MarkerBrushProperty);
        set => SetValue(MarkerBrushProperty, value);
    }

    /// <summary>Смуга займає третину висоти, маркер — усю висоту; його центр не виходить за край смуги.</summary>
    protected override void OnRender(DrawingContext drawingContext)
    {
        var trackHeight = ActualHeight / 3;
        var trackRadius = trackHeight / 2;
        var track = new Rect(0, (ActualHeight - trackHeight) / 2, ActualWidth, trackHeight);
        drawingContext.DrawRoundedRectangle(TrackBrush, null, track, trackRadius, trackRadius);

        var markerRadius = ActualHeight / 2;
        var travel = Math.Max(ActualWidth - 2 * markerRadius, 0);
        var center = new Point(markerRadius + Math.Clamp(Position, 0, 1) * travel, markerRadius);
        drawingContext.DrawEllipse(MarkerBrush, null, center, markerRadius, markerRadius);
    }
}
