using System.Globalization;
using BrewLog.Domain.Batches;
using BrewLog.Domain.Recipes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BrewLog.Data.Export;

/// <summary>
/// Етикетки партії на аркушах A4, по 8 штук (2 × 4) з лініями для різання.
/// Стиль і колір беруться зі знімка в партії, бо помічник не має доступу до рецептів.
/// </summary>
public sealed class LabelPdfGenerator
{
    /// <summary>Найбільша кількість етикеток в одному файлі.</summary>
    public const int MaxLabels = 500;

    private const string FontFamily = "Segoe UI";
    private const string AccentColor = "#5E1A28";
    private const string BorderColor = "#E7DCCD";
    private const string SecondaryTextColor = "#7A665A";
    private const string CutLineColor = "#B5A79B";
    private const float PageMargin = 20;
    private const float LabelHeight = 198;

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

    /// <summary>Створює файл PDF на кількість пляшок партії; партія має бути вже розлита.</summary>
    public void Save(Batch batch, string path)
    {
        var packaging = batch.Packaging ?? throw new InvalidOperationException("Партію ще не розлито.");
        var count = Math.Min(packaging.BottlesCount, MaxLabels);

        Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(PageMargin);
            page.DefaultTextStyle(style => style.FontFamily(FontFamily).FontSize(11));
            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                for (var index = 0; index < count; index++)
                {
                    table.Cell().Height(LabelHeight).Border(0.25f).BorderColor(CutLineColor).Padding(12).Element(container => ComposeLabel(container, batch, packaging));
                }
            });
        })).GeneratePdf(path);
    }

    private static void ComposeLabel(IContainer container, Batch batch, Packaging packaging)
    {
        var abv = batch.ActualAbv ?? batch.PlannedAbv;
        var packagedAt = batch.PackagedAt ?? packaging.PackagedAt;

        container.Border(1).BorderColor(BorderColor).Row(row =>
        {
            row.ConstantItem(16).Background(BeerColor(batch.PlannedSrm));
            row.RelativeItem().Padding(12).Column(column =>
            {
                column.Item().Text(batch.RecipeName).FontSize(22).SemiBold().FontColor(AccentColor);
                column.Item().Text(batch.StyleName ?? string.Empty).FontColor(SecondaryTextColor);
                column.Item().PaddingTop(10).Text($"ABV {abv.ToString("0.0", Culture)} %").FontSize(14).SemiBold();
                column.Item().PaddingTop(10).Text($"Партія {batch.Number}");
                column.Item().Text($"Розлито: {packagedAt.ToString("dd.MM.yyyy", Culture)}");
                column.Item().Text($"{packaging.BottleVolumeL.ToString("0.##", Culture)} л");
            });
        });
    }

    private static string BeerColor(double srm)
    {
        var (red, green, blue) = SrmColor.ToRgb(srm);
        return $"#{red:X2}{green:X2}{blue:X2}";
    }
}
