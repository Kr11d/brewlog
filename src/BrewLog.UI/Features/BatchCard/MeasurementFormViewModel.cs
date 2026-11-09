using System.Globalization;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.BatchCard;

/// <summary>
/// Форма нового виміру партії. Поля густини й температури текстові: кожне з них необов'язкове, а правила значень перевіряє Batch.
/// </summary>
public sealed partial class MeasurementFormViewModel : ObservableObject
{
    private readonly int _batchId;
    private readonly BatchService _batches;
    private readonly IClock _clock;
    private readonly DialogService _dialogs;

    [ObservableProperty]
    private DateTime? _date;

    [ObservableProperty]
    private string _timeText = string.Empty;

    [ObservableProperty]
    private string _gravityText = string.Empty;

    [ObservableProperty]
    private string _temperatureText = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    public MeasurementFormViewModel(int batchId, BatchService batches, IClock clock, DialogService dialogs)
    {
        _batchId = batchId;
        _batches = batches;
        _clock = clock;
        _dialogs = dialogs;
        ResetToNow();
    }

    /// <summary>Вимір збережено.</summary>
    public event Action? Added;

    [RelayCommand]
    private void Add()
    {
        if (Date is not { } date || !TimeSpan.TryParse(TimeText, CultureInfo.InvariantCulture, out var time) || time < TimeSpan.Zero || time.Days > 0)
        {
            _dialogs.ShowError("Вкажіть дату й час виміру, наприклад 14:30.");
            return;
        }

        if (!TryReadOptional(GravityText, out var gravity) || !TryReadOptional(TemperatureText, out var temperature))
        {
            _dialogs.ShowError("Густину й температуру вкажіть числами.");
            return;
        }

        try
        {
            _batches.AddMeasurement(_batchId, date.Date + time, gravity, temperature, Note);
            GravityText = string.Empty;
            TemperatureText = string.Empty;
            Note = string.Empty;
            ResetToNow();
            Added?.Invoke();
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }
    }

    /// <summary>Порожнє поле означає «не вимірювали»; false — у полі щось, що не є числом.</summary>
    private static bool TryReadOptional(string text, out double? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        if (!DisplayFormat.TryParseNumber(text, out var number))
        {
            return false;
        }

        value = number;
        return true;
    }

    private void ResetToNow()
    {
        var now = _clock.Now;
        Date = now.Date;
        TimeText = now.ToString("HH:mm", CultureInfo.InvariantCulture);
    }
}
