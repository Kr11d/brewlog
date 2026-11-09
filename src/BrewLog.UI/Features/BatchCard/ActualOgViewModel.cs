using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.UI.Features.Common;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.BatchCard;

/// <summary>Маленький діалог фактичної OG. Лишається відкритим, доки значення не прийнято.</summary>
public sealed partial class ActualOgViewModel
{
    private readonly int _batchId;
    private readonly BatchService _batches;
    private readonly DialogService _dialogs;

    private double _og;

    public ActualOgViewModel(int batchId, double suggestedOg, BatchService batches, DialogService dialogs)
    {
        _batchId = batchId;
        _batches = batches;
        _dialogs = dialogs;
        _og = suggestedOg;
        OgField = new NumberField(suggestedOg, value => _og = value, () => { }, DisplayFormat.Gravity);
    }

    /// <summary>Просить вікно закритися; true — значення збережено.</summary>
    public event Action<bool>? CloseRequested;

    public NumberField OgField { get; }

    [RelayCommand]
    private void Confirm()
    {
        if (!OgField.IsValid)
        {
            _dialogs.ShowError("Вкажіть густину числом, наприклад 1,048.");
            return;
        }

        try
        {
            _batches.SetActualOg(_batchId, _og);
            CloseRequested?.Invoke(true);
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }
    }
}
