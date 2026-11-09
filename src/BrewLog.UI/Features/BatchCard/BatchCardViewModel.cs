using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.UI.Features.Batches;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.BatchCard;

/// <summary>Вікно «Картка партії»: стан партії, виміри, попередження й дії за дозволами. Рецепта тут немає, тож помічник його не бачить.</summary>
public sealed partial class BatchCardViewModel : ObservableObject
{
    private readonly int _batchId;
    private readonly BatchService _batches;
    private readonly ICurrentSession _session;
    private readonly DialogService _dialogs;
    private readonly WindowService _windows;

    [ObservableProperty]
    private BatchDetailsViewModel _details = null!;

    public BatchCardViewModel(int batchId, BatchService batches, ICurrentSession session, IClock clock, DialogService dialogs, WindowService windows)
    {
        _batchId = batchId;
        _batches = batches;
        _session = session;
        _dialogs = dialogs;
        _windows = windows;

        Form = new MeasurementFormViewModel(batchId, batches, clock, dialogs);
        Form.Added += Reload;
        Reload();
    }

    /// <summary>Дані змінилися: вікно перемальовує графіки.</summary>
    public event Action? ChartsChanged;

    public MeasurementFormViewModel Form { get; }

    [RelayCommand]
    private void EnterActualOg()
    {
        if (Details.ShowActualOg && _windows.ShowActualOg(_batchId, Details.PlannedOg))
        {
            Reload();
        }
    }

    /// <summary>Переводить партію далі; запуск варки спершу підтверджує зі списком сировини.</summary>
    [RelayCommand]
    private void Advance()
    {
        if (!Details.ShowAdvance || !Details.CanAdvanceNow)
        {
            return;
        }

        try
        {
            if (Details.RequiresStock
                && !_dialogs.Confirm(WriteOffConfirmation.Describe(Details.Number, _batches.PreviewWriteOff(_batchId)), Details.AdvanceText, "Запустити варку"))
            {
                return;
            }

            _batches.Advance(_batchId);
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }

        Reload();
    }

    [RelayCommand]
    private void Package()
    {
        try
        {
            _windows.ShowBottling(_batchId);
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }

        Reload();
    }

    private void Reload()
    {
        var batch = _batches.Get(_batchId) ?? throw new DomainException("Партію не знайдено.");
        var canManage = _session.Can(Permission.ManageBatches);

        Details = new BatchDetailsViewModel(
            batch,
            _batches.GetAlerts(_batchId),
            canManage ? _batches.CanAdvance(_batchId) : null,
            canManage,
            _session.Can(Permission.AddMeasurements),
            _session.Can(Permission.PackageBatches));
        ChartsChanged?.Invoke();
    }
}
