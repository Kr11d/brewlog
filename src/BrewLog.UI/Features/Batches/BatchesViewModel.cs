using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Batches;
using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Batches;

/// <summary>
/// Сторінка партій: дошка за етапами або список, пошук, перехід на наступний етап і планування нової партії.
/// У режимі «Мої» показує партії поточного помічника та партії без виконавця. Дії без дозволу приховані.
/// </summary>
public sealed partial class BatchesViewModel : ObservableObject
{
    private const int RecentPackagedLimit = 10;

    private readonly BatchService _batches;
    private readonly UserService _users;
    private readonly ICurrentSession _session;
    private readonly IClock _clock;
    private readonly DialogService _dialogs;
    private readonly WindowService _windows;
    private readonly bool _onlyMine;

    private List<BatchItemViewModel> _allItems = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListView))]
    [NotifyPropertyChangedFor(nameof(ShowBoard))]
    [NotifyPropertyChangedFor(nameof(ShowList))]
    private bool _isBoardView = true;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<BoardColumnViewModel> _columns = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    [NotifyPropertyChangedFor(nameof(ShowBoard))]
    [NotifyPropertyChangedFor(nameof(ShowList))]
    [NotifyPropertyChangedFor(nameof(EmptyTitle))]
    [NotifyPropertyChangedFor(nameof(EmptyHint))]
    private IReadOnlyList<BatchItemViewModel> _rows = [];

    public BatchesViewModel(
        BatchService batches,
        UserService users,
        ICurrentSession session,
        IClock clock,
        DialogService dialogs,
        WindowService windows,
        bool onlyMine)
    {
        _batches = batches;
        _users = users;
        _session = session;
        _clock = clock;
        _dialogs = dialogs;
        _windows = windows;
        _onlyMine = onlyMine;

        CanManage = session.Can(Permission.ManageBatches);
        Title = onlyMine ? "Мої партії" : "Партії";
        Reload();
    }

    public string Title { get; }

    public bool CanManage { get; }

    public bool IsListView
    {
        get => !IsBoardView;
        set => IsBoardView = !value;
    }

    public bool IsEmpty => Rows.Count == 0;

    public bool ShowBoard => IsBoardView && !IsEmpty;

    public bool ShowList => !IsBoardView && !IsEmpty;

    public string EmptyTitle => _allItems.Count == 0 ? "Партій ще немає" : "Нічого не знайдено";

    public string EmptyHint => _allItems.Count == 0
        ? "Тут з'являться партії, щойно їх заплановано."
        : "Змініть текст пошуку.";

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void PlanBatch()
    {
        if (CanManage && _windows.ShowNewBatch())
        {
            Reload();
        }
    }

    [RelayCommand]
    private void Open(BatchItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        try
        {
            _windows.ShowBatchCard(item.Id);
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }

        Reload();
    }

    /// <summary>Переводить партію далі: заборону показує з причиною, а запуск варки спершу підтверджує зі списком сировини.</summary>
    [RelayCommand]
    private void Advance(BatchItemViewModel? item)
    {
        if (item is not { CanAdvance: true })
        {
            return;
        }

        try
        {
            var check = _batches.CanAdvance(item.Id);
            if (!check.Allowed)
            {
                _dialogs.ShowInfo(check.Reason ?? "Перехід на наступний етап заборонено.", item.NextStageHint);
                return;
            }

            if (item.RequiresStock
                && !_dialogs.Confirm(WriteOffConfirmation.Describe(item.Number, _batches.PreviewWriteOff(item.Id)), item.NextStageHint, "Запустити варку"))
            {
                return;
            }

            _batches.Advance(item.Id);
            Reload();
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
            Reload();
        }
    }

    private void Reload()
    {
        var now = _clock.Now;
        var names = _users.GetDisplayNames();
        var batches = _onlyMine && _session.User is { } user ? _batches.GetForAssistant(user.Id) : _batches.GetAll();

        _allItems = batches
            .Select(batch => new BatchItemViewModel(
                batch,
                batch.AssistantId is { } assistantId ? names.GetValueOrDefault(assistantId) : null,
                FermentationAnalyzer.Analyze(batch, now),
                now,
                CanManage))
            .OrderByDescending(item => item.EnteredAt)
            .ToList();
        Subtitle = $"Активних: {_allItems.Count(item => item.IsActive)}";

        ApplyFilter();
    }

    /// <summary>Застосовує пошук. Фінальна колонка показує лише останні партії, а лічильник лишається повним.</summary>
    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        var visible = _allItems.Where(item => item.Matches(search)).ToList();

        Columns = BatchStateFactory.All
            .Select(state =>
            {
                var items = visible.Where(item => item.StateCode == state.Code).ToList();
                var shown = state.IsFinal ? items.Take(RecentPackagedLimit).ToList() : items;
                return new BoardColumnViewModel(state.Code, state.DisplayName, items.Count, shown);
            })
            .ToList();
        Rows = visible;
    }
}
