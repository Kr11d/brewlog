using System.Globalization;
using BrewLog.Domain.Reports;
using ClosedXML.Excel;

namespace BrewLog.Data.Export;

/// <summary>Експорт звіту в Excel. Суми, обсяги й відсотки записуються числами, а одиниці додає формат клітинки.</summary>
public sealed class ReportExcelExporter
{
    private const string MoneyFormat = "#,##0.00\" грн\"";
    private const string LitersFormat = "0.0\" л\"";
    private const string PercentFormat = "0.0\" %\"";
    private const string SignedPercentFormat = "+0.0\" %\";-0.0\" %\";0.0\" %\"";
    private const string MonthFormat = "mm.yyyy";

    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

    /// <summary>Зберігає звіт у файл; наявний файл перезаписується.</summary>
    public void Export(MonthReport report, string path)
    {
        using var stream = File.Create(path);
        Export(report, stream);
    }

    public void Export(MonthReport report, Stream stream)
    {
        using var workbook = new XLWorkbook();
        WriteSummary(workbook.Worksheets.Add("Підсумок місяця"), report.Summary);
        WriteVolumes(workbook.Worksheets.Add("Обсяг за місяцями"), report.Volumes);
        WriteCosts(workbook.Worksheets.Add("Собівартість"), report.Costs);
        WriteAbv(workbook.Worksheets.Add("Міцність"), report.AbvComparison);
        workbook.SaveAs(stream);
    }

    private static void WriteSummary(IXLWorksheet sheet, MonthSummary summary)
    {
        WriteHeaders(sheet, "Показник", "Значення");

        sheet.Cell(2, 1).Value = "Місяць";
        sheet.Cell(2, 2).Value = new DateTime(summary.Year, summary.Month, 1).ToString("MMMM yyyy", Culture);
        sheet.Cell(3, 1).Value = "Зварено партій";
        sheet.Cell(3, 2).Value = summary.BrewedCount;
        sheet.Cell(4, 1).Value = "Розлито";
        WriteNumber(sheet.Cell(4, 2), summary.PackagedLiters, LitersFormat);
        sheet.Cell(5, 1).Value = "Середня собівартість літра";
        WriteNumber(sheet.Cell(5, 2), (double?)summary.AverageCostPerLiter, MoneyFormat);
        sheet.Cell(6, 1).Value = "Середнє відхилення ABV";
        WriteNumber(sheet.Cell(6, 2), summary.AverageAbvDeviation, SignedPercentFormat);

        Finish(sheet);
    }

    private static void WriteVolumes(IXLWorksheet sheet, IReadOnlyList<MonthlyVolume> volumes)
    {
        WriteHeaders(sheet, "Місяць", "Розлито");

        var row = 2;
        foreach (var volume in volumes)
        {
            sheet.Cell(row, 1).Value = new DateTime(volume.Year, volume.Month, 1);
            sheet.Cell(row, 1).Style.DateFormat.Format = MonthFormat;
            WriteNumber(sheet.Cell(row, 2), volume.Liters, LitersFormat);
            row++;
        }

        Finish(sheet);
    }

    private static void WriteCosts(IXLWorksheet sheet, IReadOnlyList<RecipeCostRow> costs)
    {
        WriteHeaders(sheet, "Рецепт", "Партій", "Середня, за літр", "Мінімальна, за літр", "Максимальна, за літр");

        var row = 2;
        foreach (var cost in costs)
        {
            sheet.Cell(row, 1).Value = cost.RecipeName;
            sheet.Cell(row, 2).Value = cost.BatchCount;
            WriteNumber(sheet.Cell(row, 3), (double)cost.AverageCostPerLiter, MoneyFormat);
            WriteNumber(sheet.Cell(row, 4), (double)cost.MinCostPerLiter, MoneyFormat);
            WriteNumber(sheet.Cell(row, 5), (double)cost.MaxCostPerLiter, MoneyFormat);
            row++;
        }

        Finish(sheet);
    }

    private static void WriteAbv(IXLWorksheet sheet, IReadOnlyList<AbvComparisonRow> rows)
    {
        WriteHeaders(sheet, "Партія", "Рецепт", "Планова ABV", "Фактична ABV", "Різниця");

        var row = 2;
        foreach (var item in rows)
        {
            sheet.Cell(row, 1).Value = item.Number;
            sheet.Cell(row, 2).Value = item.RecipeName;
            WriteNumber(sheet.Cell(row, 3), item.PlannedAbv, PercentFormat);
            WriteNumber(sheet.Cell(row, 4), item.ActualAbv, PercentFormat);
            WriteNumber(sheet.Cell(row, 5), item.Difference, SignedPercentFormat);
            row++;
        }

        Finish(sheet);
    }

    private static void WriteHeaders(IXLWorksheet sheet, params string[] headers)
    {
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        sheet.Row(1).Style.Font.Bold = true;
    }

    /// <summary>Порожнє значення лишає клітинку порожньою: у місяці без розливів середніх немає.</summary>
    private static void WriteNumber(IXLCell cell, double? value, string format)
    {
        if (value is { } number)
        {
            cell.Value = number;
            cell.Style.NumberFormat.Format = format;
        }
    }

    private static void Finish(IXLWorksheet sheet)
    {
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
    }
}
