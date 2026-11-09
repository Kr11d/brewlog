using System.Globalization;
using BrewLog.Domain.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BrewLog.Data.Export;

/// <summary>Звіт за місяць у PDF. Зображення графіка готує інтерфейс, бо Data не залежить від ScottPlot.</summary>
public sealed class ReportPdfExporter
{
    private const string FontFamily = "Segoe UI";
    private const string AccentColor = "#5E1A28";
    private const string BorderColor = "#E7DCCD";
    private const string MutedRowColor = "#F5EEE4";
    private const string SurfaceColor = "#FFFFFF";
    private const string SecondaryTextColor = "#7A665A";

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

    /// <summary>Створює файл PDF; наявний файл перезаписується.</summary>
    /// <param name="chartPng">Графік обсягу в форматі PNG.</param>
    public void Save(MonthReport report, byte[] chartPng, DateTime generatedAt, string path) =>
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(style => style.FontFamily(FontFamily).FontSize(10));

            page.Header().Text($"BrewLog — звіт за {MonthTitle(report.Summary)}").FontSize(18).SemiBold().FontColor(AccentColor);
            page.Content().PaddingVertical(14).Column(column =>
            {
                column.Spacing(16);
                column.Item().Element(container => ComposeTiles(container, report.Summary));
                column.Item().Text("Обсяг за 12 місяців").FontSize(12).SemiBold().FontColor(AccentColor);
                column.Item().Image(chartPng).FitWidth();
                column.Item().Element(container => ComposeCosts(container, report.Costs));
                column.Item().Element(container => ComposeAbv(container, report.AbvComparison));
            });
            page.Footer().Element(container => ComposeFooter(container, generatedAt));
        })).GeneratePdf(path);

    private static string MonthTitle(MonthSummary summary) => new DateTime(summary.Year, summary.Month, 1).ToString("MMMM yyyy", Culture);

    private static void ComposeTiles(IContainer container, MonthSummary summary) => container.Row(row =>
    {
        row.Spacing(8);
        AddTile(row, summary.BrewedCount.ToString(Culture), "Зварено партій");
        AddTile(row, Number(summary.PackagedLiters, "0.0"), "Розлито, л");
        AddTile(row, summary.AverageCostPerLiter is { } cost ? Money(cost) : "—", "Собівартість, грн/л");
        AddTile(row, summary.AverageAbvDeviation is { } deviation ? $"{Number(deviation, "+0.0;−0.0;0.0")} %" : "—", "Відхилення ABV");
    });

    private static void AddTile(RowDescriptor row, string value, string caption) =>
        row.RelativeItem().Border(0.75f).BorderColor(BorderColor).Padding(10).Column(column =>
        {
            column.Item().Text(value).FontSize(16).SemiBold();
            column.Item().Text(caption).FontSize(9).FontColor(SecondaryTextColor);
        });

    private static void ComposeCosts(IContainer container, IReadOnlyList<RecipeCostRow> costs) => ComposeTable(
        container,
        "Собівартість за рецептами, грн/л",
        [("Рецепт", 4, false), ("Партій", 1, true), ("Середня", 2, true), ("Мін.", 2, true), ("Макс.", 2, true)],
        costs.Select(cost => new[]
        {
            cost.RecipeName,
            cost.BatchCount.ToString(Culture),
            Money(cost.AverageCostPerLiter),
            Money(cost.MinCostPerLiter),
            Money(cost.MaxCostPerLiter),
        }));

    private static void ComposeAbv(IContainer container, IReadOnlyList<AbvComparisonRow> rows) => ComposeTable(
        container,
        "Планова та фактична міцність, %",
        [("Партія", 3, false), ("Рецепт", 4, false), ("План", 2, true), ("Факт", 2, true), ("Різниця", 2, true)],
        rows.Select(row => new[]
        {
            row.Number,
            row.RecipeName,
            Number(row.PlannedAbv, "0.0"),
            Number(row.ActualAbv, "0.0"),
            Number(row.Difference, "+0.0;−0.0;0.0"),
        }));

    private static void ComposeTable(IContainer container, string title, (string Title, float Width, bool AlignRight)[] columns, IEnumerable<string[]> rows) => container.Column(column =>
    {
        column.Item().PaddingBottom(4).Text(title).FontSize(12).SemiBold().FontColor(AccentColor);
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(definition =>
            {
                foreach (var (_, width, _) in columns)
                {
                    definition.RelativeColumn(width);
                }
            });

            table.Header(header =>
            {
                foreach (var (name, _, alignRight) in columns)
                {
                    var cell = header.Cell().BorderBottom(1).BorderColor(AccentColor).Padding(4);
                    (alignRight ? cell.AlignRight() : cell).Text(name).SemiBold().FontColor(AccentColor);
                }
            });

            foreach (var (values, rowIndex) in rows.Select((values, rowIndex) => (values, rowIndex)))
            {
                var background = rowIndex % 2 == 1 ? MutedRowColor : SurfaceColor;
                for (var index = 0; index < values.Length; index++)
                {
                    var cell = table.Cell().Background(background).Padding(4);
                    (columns[index].AlignRight ? cell.AlignRight() : cell).Text(values[index]);
                }
            }
        });
    });

    private static void ComposeFooter(IContainer container, DateTime generatedAt) => container.Row(row =>
    {
        row.RelativeItem().Text($"Сформовано {generatedAt.ToString("dd.MM.yyyy HH:mm", Culture)}").FontSize(9).FontColor(SecondaryTextColor);
        row.RelativeItem().AlignRight().Text(text =>
        {
            text.DefaultTextStyle(style => style.FontSize(9).FontColor(SecondaryTextColor));
            text.Span("Сторінка ");
            text.CurrentPageNumber();
            text.Span(" з ");
            text.TotalPages();
        });
    });

    private static string Money(decimal value) => value.ToString("#,0.00", Culture);

    private static string Number(double value, string format) => value.ToString(format, Culture);
}
