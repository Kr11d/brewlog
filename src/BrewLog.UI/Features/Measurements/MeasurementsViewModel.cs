using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Batches;
using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.UI.Features.BatchCard;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Measurements;

/// <summary>
/// Робоче місце помічника: його активні партії й партії без виконавця, спершу ті, які сьогодні ще не міряли,
/// і швидка форма нового виміру для вибраної партії.
/// </summary>
public sealed partial class MeasurementsViewModel : ObservableObject
{
    private readonly BatchService _batches;
    private readonly ICurrentSession _session;
    private readonly IClock _clock;
    private readonly DialogService _dialogs;
    private readonly WindowService _windows;

    private int? _formBatchId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRows))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private IReadOnlyList<MeasurementBatchRowViewModel> _rows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private MeasurementBatchRowViewModel? _selectedRow;

    [ObservableProperty]
    private MeasurementFormViewModel? _form;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    public MeasurementsViewModel(BatchService batches, ICurrentSession session, IClock clock, DialogService dialogs, WindowService windows)
    {
        _batches = batches;
        _session = session;
        _clock = clock;
        _dialogs = dialogs;
        _windows = windows;
        Reload();
    }

    public bool HasRows => Rows.Count > 0;

    public bool IsEmpty => Rows.Count == 0;

    public bool HasSelection => SelectedRow is not null;

    /// <summary>Нова форма — лише для іншої партії: після перечитування рядків той самий вибір не скидає введене.</summary>
    partial void OnSelectedRowChanged(MeasurementBatchRowViewModel? value)
    {
        if (value?.Id == _formBatchId)
        {
            return;
        }

        _formBatchId = value?.Id;
        Form = value is null ? null : CreateForm(value.Id);
    }

    [RelayCommand]
    private void OpenCard()
    {
        if (SelectedRow is not { } row)
        {
            return;
        }

        try
        {
            _windows.ShowBatchCard(row.Id);
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }

        Reload();
    }

    private MeasurementFormViewModel CreateForm(int batchId)
    {
        var form = new MeasurementFormViewModel(batchId, _batches, _clock, _dialogs);
        form.Added += Reload;
        return form;
    }

    /// <summary>Перечитує партії; вибраною лишається та сама, якщо вона ще активна.</summary>
    private void Reload()
    {
        var selectedId = SelectedRow?.Id;
        var now = _clock.Now;
        var batches = _session.User is { } user ? _batches.GetForAssistant(user.Id) : [];

        Rows = batches
            .Where(batch => batch.State.AllowsMeasurements)
            .Select(batch => new MeasurementBatchRowViewModel(batch, FermentationAnalyzer.Analyze(batch, now), now))
            .OrderByDescending(row => row.NeedsAttention)
            .ThenBy(row => row.Number, StringComparer.Ordinal)
            .ToList();
        SelectedRow = Rows.FirstOrDefault(row => row.Id == selectedId) ?? Rows.FirstOrDefault();

        var unmeasured = Rows.Count(row => row.NeedsAttention);
        Subtitle = $"{DisplayFormat.Count(Rows.Count, "активна партія", "активні партії", "активних партій")} · сьогодні ще не міряли: {unmeasured}";
    }
}
