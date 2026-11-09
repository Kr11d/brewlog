using BrewLog.Data.Export;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Reports;
using BrewLog.Domain.Services;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Reports;

/// <summary>Сторінка «Звіти»: показники вибраного місяця, обсяг за 12 місяців, собівартість і порівняння міцності.</summary>
public sealed partial class ReportsViewModel : ObservableObject
{
    private const int ChartImageWidth = 1200;
    private const int ChartImageHeight = 420;

    private readonly ReportService _reports;
    private readonly IClock _clock;
    private readonly DialogService _dialogs;
    private readonly ReportExcelExporter _excel;
    private readonly ReportPdfExporter _pdf;
    private readonly DateTime _currentMonth;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportExcelCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportPdfCommand))]
    private bool _isExporting;

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
    [NotifyPropertyChangedFor(nameof(HasNoCostRows))]
    private IReadOnlyList<RecipeCostViewRow> _costRows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoAbvRows))]
    private IReadOnlyList<AbvComparisonViewRow> _abvRows = [];

    public ReportsViewModel(ReportService reports, IClock clock, DialogService dialogs, ReportExcelExporter excel, ReportPdfExporter pdf)
    {
        _reports = reports;
        _clock = clock;
        _dialogs = dialogs;
        _excel = excel;
        _pdf = pdf;

        var now = clock.Now;
        _currentMonth = new DateTime(now.Year, now.Month, 1);
        _selectedMonth = _currentMonth;
        Reload();
    }

    /// <summary>Дані змінилися: сторінка перемальовує графік.</summary>
    public event Action? ChartChanged;

    public string MonthTitle => DisplayFormat.MonthAndYear(SelectedMonth);

    public bool HasNoCostRows => CostRows.Count == 0;

    public bool HasNoAbvRows => AbvRows.Count == 0;

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

    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportExcelAsync()
    {
        var path = _dialogs.PickFileToSave("Excel (*.xlsx)|*.xlsx", "Експорт звіту", SuggestedName("xlsx"));
        if (path is null)
        {
            return;
        }

        var report = LoadReport();
        await RunExportAsync(() => _excel.Export(report, path), path);
    }

    /// <summary>Зображення графіка готується тут: ScottPlot бере кольори з теми в потоці інтерфейсу.</summary>
    [RelayCommand(CanExecute = nameof(CanExport))]
    private async Task ExportPdfAsync()
    {
        var path = _dialogs.PickFileToSave("PDF (*.pdf)|*.pdf", "Експорт звіту", SuggestedName("pdf"));
        if (path is null)
        {
            return;
        }

        var report = LoadReport();
        var chart = VolumeChart.RenderPng(Bars, ChartImageWidth, ChartImageHeight);
        var generatedAt = _clock.Now;
        await RunExportAsync(() => _pdf.Save(report, chart, generatedAt, path), path);
    }

    /// <summary>Обидві кнопки вимкнені, доки створюється будь-який із файлів.</summary>
    private async Task RunExportAsync(Action save, string path)
    {
        IsExporting = true;
        try
        {
            await _dialogs.SaveAndOpenAsync(save, path);
        }
        finally
        {
            IsExporting = false;
        }
    }

    private MonthReport LoadReport() => _reports.GetMonthReport(SelectedMonth.Year, SelectedMonth.Month);

    private string SuggestedName(string extension) => $"Звіт-{DisplayFormat.MonthAndYear(SelectedMonth)}.{extension}";

    private bool CanExport() => !IsExporting;

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
