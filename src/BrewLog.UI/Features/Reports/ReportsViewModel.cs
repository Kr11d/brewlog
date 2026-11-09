using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Services;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Reports;

/// <summary>Сторінка «Звіти»: показники вибраного місяця, обсяг за 12 місяців, собівартість і порівняння міцності.</summary>
public sealed partial class ReportsViewModel : ObservableObject
{
    private readonly ReportService _reports;
    private readonly DialogService _dialogs;
    private readonly DateTime _currentMonth;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthTitle))]
    [NotifyCanExecuteChangedFor(nameof(NextMonthCommand))]
    private DateTime _selectedMonth;

    [ObservableProperty]
    private string _brewedText = string.Empty;

    [ObservableProperty]
    private string _packagedText = string.Empty;

    [ObservableProperty]
    private string _costText = string.Empty;

    [ObservableProperty]
    private string _abvDeviationText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<MonthBar> _bars = [];

    [ObservableProperty]
    private IReadOnlyList<RecipeCostViewRow> _costRows = [];

    [ObservableProperty]
    private IReadOnlyList<AbvComparisonViewRow> _abvRows = [];

    public ReportsViewModel(ReportService reports, IClock clock, DialogService dialogs)
    {
        _reports = reports;
        _dialogs = dialogs;

        var now = clock.Now;
        _currentMonth = new DateTime(now.Year, now.Month, 1);
        _selectedMonth = _currentMonth;
        Reload();
    }

    /// <summary>Дані змінилися: сторінка перемальовує графік.</summary>
    public event Action? ChartChanged;

    public string MonthTitle => DisplayFormat.MonthAndYear(SelectedMonth);

    [RelayCommand]
    private void PreviousMonth()
    {
        SelectedMonth = SelectedMonth.AddMonths(-1);
        Reload();
    }

    /// <summary>Майбутні місяці не показуються: у них ще немає даних.</summary>
    [RelayCommand(CanExecute = nameof(CanGoToNextMonth))]
    private void NextMonth()
    {
        SelectedMonth = SelectedMonth.AddMonths(1);
        Reload();
    }

    [RelayCommand]
    private void ExportExcel() => _dialogs.ShowInfo("Буде в A11");

    [RelayCommand]
    private void ExportPdf() => _dialogs.ShowInfo("Буде в A11");

    private bool CanGoToNextMonth() => SelectedMonth < _currentMonth;

    private void Reload()
    {
        var summary = _reports.GetMonthSummary(SelectedMonth.Year, SelectedMonth.Month);
        BrewedText = DisplayFormat.Integer(summary.BrewedCount);
        PackagedText = DisplayFormat.Number(summary.PackagedLiters);
        CostText = summary.AverageCostPerLiter is { } cost ? DisplayFormat.MoneyValue(cost) : "—";
        AbvDeviationText = summary.AverageAbvDeviation is { } deviation ? DisplayFormat.SignedPercent(deviation) : "—";

        Bars = _reports.GetVolumeByMonth(SelectedMonth.Year, SelectedMonth.Month)
            .Select(volume => (Month: new DateTime(volume.Year, volume.Month, 1), volume.Liters))
            .Select(volume => new MonthBar(DisplayFormat.ShortMonth(volume.Month), volume.Liters, volume.Month == SelectedMonth))
            .ToList();
        CostRows = _reports.GetCostByRecipe().Select(row => new RecipeCostViewRow(row)).ToList();
        AbvRows = _reports.GetAbvComparison().Select(row => new AbvComparisonViewRow(row)).ToList();

        ChartChanged?.Invoke();
    }
}
