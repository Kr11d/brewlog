using System.Globalization;
using BrewLog.Data.Export;
using BrewLog.Domain.Reports;
using ClosedXML.Excel;

namespace BrewLog.Tests.Data;

public sealed class ReportExcelExporterTests
{
    /// <summary>Експорт під uk-UA: десяткова кома не має перетворити суми й відсотки на текст.</summary>
    [Fact]
    public void Export_Report_WritesFourSheetsWithAmountsAsNumbers()
    {
        var report = new MonthReport(
            new MonthSummary(2026, 3, 2, 90.5, 45.25m, -0.25),
            [new MonthlyVolume(2026, 2, 40), new MonthlyVolume(2026, 3, 90.5)],
            [new RecipeCostRow("Czech Pils", 2, 45.25m, 40.5m, 50m)],
            [new AbvComparisonRow("BL-2026-001", "Czech Pils", 4.7, 4.95, 0.25)]);

        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("uk-UA");
        try
        {
            using var stream = new MemoryStream();
            new ReportExcelExporter().Export(report, stream);
            stream.Position = 0;
            using var workbook = new XLWorkbook(stream);

            Assert.Equal(
                ["Підсумок місяця", "Обсяг за місяцями", "Собівартість", "Міцність"],
                workbook.Worksheets.Select(sheet => sheet.Name));

            AssertNumber(workbook.Worksheet("Підсумок місяця").Cell(4, 2), 90.5);
            AssertNumber(workbook.Worksheet("Підсумок місяця").Cell(5, 2), 45.25);
            AssertNumber(workbook.Worksheet("Підсумок місяця").Cell(6, 2), -0.25);
            AssertNumber(workbook.Worksheet("Обсяг за місяцями").Cell(3, 2), 90.5);
            AssertNumber(workbook.Worksheet("Собівартість").Cell(2, 3), 45.25);
            AssertNumber(workbook.Worksheet("Міцність").Cell(2, 4), 4.95);
            AssertNumber(workbook.Worksheet("Міцність").Cell(2, 5), 0.25);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    private static void AssertNumber(IXLCell cell, double expected)
    {
        Assert.Equal(XLDataType.Number, cell.DataType);
        Assert.Equal(expected, cell.GetDouble(), 0.0001);
    }
}
