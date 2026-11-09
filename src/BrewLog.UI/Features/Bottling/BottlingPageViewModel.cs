using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Bottling;

/// <summary>Сторінка «Розлив»: партії в дозріванні, доступні користувачу. Сервіс сам відсікає чужі партії помічника.</summary>
public sealed partial class BottlingPageViewModel : ObservableObject
{
    private readonly BatchService _batches;
    private readonly IClock _clock;
    private readonly DialogService _dialogs;
    private readonly WindowService _windows;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRows))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private IReadOnlyList<BottlingRowViewModel> _rows = [];

    [ObservableProperty]
    private string _subtitle = string.Empty;

    public BottlingPageViewModel(BatchService batches, IClock clock, DialogService dialogs, WindowService windows)
    {
        _batches = batches;
        _clock = clock;
        _dialogs = dialogs;
        _windows = windows;
        Reload();
    }

    public bool HasRows => Rows.Count > 0;

    public bool IsEmpty => Rows.Count == 0;

    [RelayCommand]
    private void Open(BottlingRowViewModel? row)
    {
        if (row is null)
        {
            return;
        }

        try
        {
            _windows.ShowBottling(row.Id);
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }

        Reload();
    }

    private void Reload()
    {
        var now = _clock.Now;
        Rows = _batches.GetAll()
            .Where(batch => batch.State.AllowsPackaging)
            .OrderBy(batch => batch.Number, StringComparer.Ordinal)
            .Select(batch => new BottlingRowViewModel(batch, now))
            .ToList();
        Subtitle = DisplayFormat.Count(Rows.Count, "партія готова до розливу", "партії готові до розливу", "партій готові до розливу");
    }
}
