using BrewLog.Domain.Inventory;
using ClosedXML.Excel;

namespace BrewLog.Data.Export;

/// <summary>Експорт складу в Excel: аркуш залишків (як таблиця сторінки) й аркуш усіх партій сировини.</summary>
public sealed class StockExcelExporter
{
    /// <summary>Звичайний формат: «0.###» лишав би кому після цілого числа («9,»).</summary>
    private const string QuantityFormat = "General";

    private const string MoneyFormat = "#,##0.00";
    private const string DateFormat = "dd.mm.yyyy";

    /// <param name="positions">Позиції складу.</param>
    /// <param name="lots">Усі партії, зокрема вичерпані.</param>
    /// <param name="suppliers">Назви постачальників за ідентифікатором.</param>
    /// <param name="path">Шлях до файлу; наявний файл перезаписується.</param>
    public void Export(IReadOnlyList<StockPosition> positions, IReadOnlyList<StockLot> lots, IReadOnlyDictionary<int, string> suppliers, string path)
    {
        using var workbook = new XLWorkbook();
        WriteBalances(workbook.Worksheets.Add("Залишки"), positions);
        WriteLots(workbook.Worksheets.Add("Партії складу"), positions, lots, suppliers);
        workbook.SaveAs(path);
    }

    private static void WriteBalances(IXLWorksheet sheet, IReadOnlyList<StockPosition> positions)
    {
        string[] headers = ["Назва", "Тип", "Доступно", "Од.", "Мінімум", "Найближчий строк", "Середня ціна, грн", "Статус"];
        WriteHeaders(sheet, headers);

        var row = 2;
        foreach (var position in positions)
        {
            sheet.Cell(row, 1).Value = position.Item.Name;
            sheet.Cell(row, 2).Value = position.Item.KindDisplayName;
            WriteNumber(sheet.Cell(row, 3), position.Available, QuantityFormat);
            sheet.Cell(row, 4).Value = position.Item.Unit;
            WriteNumber(sheet.Cell(row, 5), position.MinStock, QuantityFormat);
            WriteDate(sheet.Cell(row, 6), position.NearestExpiry);
            if (position.AverageUnitPrice is { } price)
            {
                WriteNumber(sheet.Cell(row, 7), (double)price, MoneyFormat);
            }

            sheet.Cell(row, 8).Value = position.StatusText;
            row++;
        }

        Finish(sheet, row - 1, headers.Length);
    }

    private static void WriteLots(IXLWorksheet sheet, IReadOnlyList<StockPosition> positions, IReadOnlyList<StockLot> lots, IReadOnlyDictionary<int, string> suppliers)
    {
        string[] headers = ["Позиція", "Дата приходу", "Постачальник", "Отримано", "Залишок", "Од.", "Ціна, грн", "Строк", "Код партії"];
        WriteHeaders(sheet, headers);

        var items = positions.ToDictionary(position => position.Item.CatalogId, position => position.Item);
        var row = 2;
        foreach (var lot in lots.OrderBy(lot => lot.ReceivedAt))
        {
            var item = items.GetValueOrDefault(lot.CatalogId);
            sheet.Cell(row, 1).Value = item?.Name ?? string.Empty;
            WriteDate(sheet.Cell(row, 2), lot.ReceivedAt);
            sheet.Cell(row, 3).Value = lot.SupplierId is { } supplierId ? suppliers.GetValueOrDefault(supplierId, string.Empty) : string.Empty;
            WriteNumber(sheet.Cell(row, 4), lot.QuantityReceived, QuantityFormat);
            WriteNumber(sheet.Cell(row, 5), lot.QuantityLeft, QuantityFormat);
            sheet.Cell(row, 6).Value = item?.Unit ?? string.Empty;
            WriteNumber(sheet.Cell(row, 7), (double)lot.UnitPrice, MoneyFormat);
            WriteDate(sheet.Cell(row, 8), lot.ExpiresAt);
            sheet.Cell(row, 9).Value = lot.LotCode ?? string.Empty;
            row++;
        }

        Finish(sheet, row - 1, headers.Length);
    }

    private static void WriteHeaders(IXLWorksheet sheet, string[] headers)
    {
        for (var column = 0; column < headers.Length; column++)
        {
            sheet.Cell(1, column + 1).Value = headers[column];
        }

        sheet.Row(1).Style.Font.Bold = true;
    }

    private static void WriteNumber(IXLCell cell, double value, string format)
    {
        cell.Value = value;
        cell.Style.NumberFormat.Format = format;
    }

    private static void WriteDate(IXLCell cell, DateTime? value)
    {
        if (value is { } date)
        {
            cell.Value = date;
            cell.Style.DateFormat.Format = DateFormat;
        }
    }

    private static void Finish(IXLWorksheet sheet, int lastRow, int lastColumn)
    {
        sheet.Range(1, 1, lastRow, lastColumn).SetAutoFilter();
        sheet.Columns().AdjustToContents();
    }
}
