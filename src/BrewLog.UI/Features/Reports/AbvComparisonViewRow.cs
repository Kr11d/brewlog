using BrewLog.Domain.Reports;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.Reports;

/// <summary>Рядок порівняння міцності; колір різниці тема вибирає за ключем.</summary>
public sealed class AbvComparisonViewRow
{
    private const double GoodDifference = 0.3;
    private const double AcceptableDifference = 0.7;
    private const double Tolerance = 1e-9;

    public AbvComparisonViewRow(AbvComparisonRow row)
    {
        Number = row.Number;
        RecipeName = row.RecipeName;
        PlannedText = DisplayFormat.Abv(row.PlannedAbv);
        ActualText = DisplayFormat.Abv(row.ActualAbv);
        Difference = row.Difference;
        DifferenceText = DisplayFormat.SignedPercent(row.Difference);

        var size = Math.Abs(row.Difference);
        DifferenceBrush = size <= GoodDifference + Tolerance ? "Success" : size <= AcceptableDifference + Tolerance ? "Warning" : "Danger";
    }

    public string Number { get; }

    public string RecipeName { get; }

    public string PlannedText { get; }

    public string ActualText { get; }

    public double Difference { get; }

    public string DifferenceText { get; }

    /// <summary>Ключ пензля теми: Success, Warning або Danger.</summary>
    public string DifferenceBrush { get; }
}
