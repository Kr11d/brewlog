using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Batches;
using BrewLog.Domain.Reports;
using BrewLog.Domain.Users;

namespace BrewLog.Domain.Services;

/// <summary>Звіти власника за партіями: показники місяця, обсяг за рік, собівартість за рецептами, планова й фактична міцність.</summary>
public sealed class ReportService
{
    private const int VolumeMonths = 12;

    private readonly IBatchRepository _batches;
    private readonly ICurrentSession _session;

    public ReportService(IBatchRepository batches, ICurrentSession session)
    {
        _batches = batches;
        _session = session;
    }

    /// <summary>Повний звіт за місяць для експорту.</summary>
    public MonthReport GetMonthReport(int year, int month) =>
        new(GetMonthSummary(year, month), GetVolumeByMonth(year, month), GetCostByRecipe(), GetAbvComparison());

    /// <summary>Зварене рахується за датою варки, усе інше — за датою розливу в цьому місяці.</summary>
    public MonthSummary GetMonthSummary(int year, int month)
    {
        _session.Require(Permission.ViewReports);

        var batches = _batches.GetAll();
        var packaged = batches.Where(batch => IsIn(batch.PackagedAt, year, month) && batch.Packaging is not null).ToList();
        var costs = packaged.Select(batch => batch.CostPerLiter).OfType<decimal>().ToList();
        var deviations = packaged.Where(batch => batch.ActualAbv.HasValue).Select(batch => batch.ActualAbv!.Value - batch.PlannedAbv).ToList();

        return new MonthSummary(
            year,
            month,
            batches.Count(batch => IsIn(batch.BrewedAt, year, month)),
            packaged.Sum(batch => batch.Packaging!.PackagedVolumeL),
            costs.Count > 0 ? costs.Average() : null,
            deviations.Count > 0 ? deviations.Average() : null);
    }

    /// <summary>Розлитий обсяг за 12 місяців, що закінчуються вказаним; місяці без розливів дають 0.</summary>
    public IReadOnlyList<MonthlyVolume> GetVolumeByMonth(int toYear, int toMonth)
    {
        _session.Require(Permission.ViewReports);

        var batches = _batches.GetAll();
        var last = new DateTime(toYear, toMonth, 1);
        return Enumerable.Range(0, VolumeMonths)
            .Select(index => last.AddMonths(index - VolumeMonths + 1))
            .Select(month => new MonthlyVolume(
                month.Year,
                month.Month,
                batches
                    .Where(batch => IsIn(batch.PackagedAt, month.Year, month.Month))
                    .Sum(batch => batch.Packaging?.PackagedVolumeL ?? 0)))
            .ToList();
    }

    /// <summary>Лише розлиті партії: без розливу собівартість літра невідома. Групування за рецептом, а не за назвою, бо рецепт можуть перейменувати.</summary>
    public IReadOnlyList<RecipeCostRow> GetCostByRecipe()
    {
        _session.Require(Permission.ViewReports);

        return _batches.GetAll()
            .Where(batch => batch.CostPerLiter.HasValue)
            .GroupBy(batch => batch.RecipeId)
            .Select(group =>
            {
                var costs = group.Select(batch => batch.CostPerLiter!.Value).ToList();
                return new RecipeCostRow(group.First().RecipeName, costs.Count, costs.Average(), costs.Min(), costs.Max());
            })
            .OrderBy(row => row.RecipeName, StringComparer.CurrentCulture)
            .ToList();
    }

    /// <summary>Партії з відомою фактичною міцністю.</summary>
    public IReadOnlyList<AbvComparisonRow> GetAbvComparison()
    {
        _session.Require(Permission.ViewReports);

        return _batches.GetAll()
            .Where(batch => batch.ActualAbv.HasValue)
            .OrderBy(batch => batch.Number, StringComparer.Ordinal)
            .Select(ToComparison)
            .ToList();
    }

    private static AbvComparisonRow ToComparison(Batch batch) =>
        new(batch.Number, batch.RecipeName, batch.PlannedAbv, batch.ActualAbv!.Value, batch.ActualAbv.Value - batch.PlannedAbv);

    private static bool IsIn(DateTime? date, int year, int month) => date?.Year == year && date?.Month == month;
}
