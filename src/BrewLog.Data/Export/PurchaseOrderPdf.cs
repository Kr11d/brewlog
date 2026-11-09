using System.Globalization;
using BrewLog.Domain.Inventory;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BrewLog.Data.Export;

/// <summary>Друкована форма замовлення постачальнику (A4, QuestPDF). Кольори — з палітри «Темне пиво» (HANDOFF §9.2).</summary>
public sealed class PurchaseOrderPdf
{
    private const string FontFamily = "Segoe UI";
    private const string AccentColor = "#5E1A28";
    private const string MutedRowColor = "#F5EEE4";
    private const string SurfaceColor = "#FFFFFF";
    private const string SecondaryTextColor = "#7A665A";

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

    private static readonly (string Title, bool AlignRight)[] Headers =
    [
        ("№", false),
        ("Позиція", false),
        ("Тип", false),
        ("Кількість", true),
        ("Од.", false),
        ("Ціна, грн", true),
        ("Сума, грн", true),
    ];

    /// <summary>Створює файл PDF; наявний файл перезаписується.</summary>
    public void Save(PurchaseOrder order, string path) =>
        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(style => style.FontFamily(FontFamily).FontSize(10));
            page.Header().Element(container => ComposeHeader(container, order));
            page.Content().PaddingVertical(16).Element(container => ComposeLines(container, order));
            page.Footer().Element(ComposeSignature);
        })).GeneratePdf(path);

    private static void ComposeHeader(IContainer container, PurchaseOrder order) => container.Column(column =>
    {
        column.Item().Text($"Замовлення постачальнику № {order.Number}").FontSize(18).SemiBold().FontColor(AccentColor);
        column.Item().Text($"Дата: {order.CreatedAt.ToString("dd.MM.yyyy", Culture)}").FontColor(SecondaryTextColor);
        column.Item().PaddingTop(12).Text("Від: броварня BrewLog");
        column.Item().Text($"Кому: {order.Supplier?.Name ?? "постачальника не вказано"}");
    });

    private static void ComposeLines(IContainer container, PurchaseOrder order) => container.Column(column =>
    {
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(28);
                columns.RelativeColumn(4);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.ConstantColumn(36);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                foreach (var (title, alignRight) in Headers)
                {
                    var cell = header.Cell().BorderBottom(1).BorderColor(AccentColor).Padding(4);
                    (alignRight ? cell.AlignRight() : cell).Text(title).SemiBold().FontColor(AccentColor);
                }
            });

            foreach (var (line, index) in order.Lines.Select((line, index) => (line, index)))
            {
                var background = index % 2 == 1 ? MutedRowColor : SurfaceColor;
                AddCell(table, background, (index + 1).ToString(Culture));
                AddCell(table, background, line.Name);
                AddCell(table, background, line.KindName);
                AddCell(table, background, line.Quantity.ToString("0.###", Culture), alignRight: true);
                AddCell(table, background, line.Unit);
                AddCell(table, background, Money(line.UnitPrice), alignRight: true);
                AddCell(table, background, Money(line.Total), alignRight: true);
            }
        });

        column.Item().PaddingTop(12).AlignRight().Text($"Разом: {Money(order.Total)} грн").FontSize(12).SemiBold().FontColor(AccentColor);
    });

    private static void ComposeSignature(IContainer container) => container.Row(row =>
    {
        row.RelativeItem().Text("Підпис: ____________________");
        row.RelativeItem().AlignRight().Text("Дата: ____________________");
    });

    private static void AddCell(TableDescriptor table, string background, string text, bool alignRight = false)
    {
        var cell = table.Cell().Background(background).Padding(4);
        (alignRight ? cell.AlignRight() : cell).Text(text);
    }

    private static string Money(decimal value) => value.ToString("#,0.00", Culture);
}
