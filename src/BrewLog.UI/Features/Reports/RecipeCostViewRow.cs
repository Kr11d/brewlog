using BrewLog.Domain.Reports;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.Reports;

/// <summary>Рядок таблиці собівартості за рецептами.</summary>
public sealed class RecipeCostViewRow(RecipeCostRow row)
{
    public string RecipeName { get; } = row.RecipeName;

    public int BatchCount { get; } = row.BatchCount;

    public decimal Average { get; } = row.AverageCostPerLiter;

    public string AverageText { get; } = DisplayFormat.MoneyValue(row.AverageCostPerLiter);

    public string MinText { get; } = DisplayFormat.MoneyValue(row.MinCostPerLiter);

    public string MaxText { get; } = DisplayFormat.MoneyValue(row.MaxCostPerLiter);
}
