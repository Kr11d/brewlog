using BrewLog.Domain.Common;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.References;

/// <summary>
/// Секція діалога «Довідники»: список із пошуком і форма вибраної або нової позиції.
/// Спільна для стилів та інгредієнтів (узагальнений клас): що читати, показувати й зберігати, вирішують нащадки.
/// Правила перевіряє CatalogService; тут лише форма й підтвердження незбережених змін.
/// </summary>
public abstract partial class ReferenceSectionViewModel<TEntity> : ObservableObject
    where TEntity : class, IEntity
{
    private readonly DialogService _dialogs;
    private readonly string _newItemTitle;
    private readonly string _deleteTitle;

    private Dictionary<int, TEntity> _items = [];
    private List<ReferenceRowViewModel> _allRows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmptyResult))]
    private IReadOnlyList<ReferenceRowViewModel> _rows = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExisting))]
    private ReferenceRowViewModel? _selectedRow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExisting))]
    private bool _isNew;

    [ObservableProperty]
    private string _formTitle = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormVisible))]
    private ReferenceFormViewModel<TEntity>? _form;

    /// <param name="newItemTitle">Заголовок форми нової позиції, наприклад «Новий стиль».</param>
    /// <param name="deleteTitle">Заголовок підтвердження видалення.</param>
    /// <param name="searchHint">Підказка в полі пошуку.</param>
    protected ReferenceSectionViewModel(DialogService dialogs, string newItemTitle, string deleteTitle, string searchHint)
    {
        _dialogs = dialogs;
        _newItemTitle = newItemTitle;
        _deleteTitle = deleteTitle;
        SearchHint = searchHint;
    }

    public string SearchHint { get; }

    public string NewItemButton => $"+ {_newItemTitle}";

    public bool IsExisting => !IsNew && SelectedRow is not null;

    public bool IsFormVisible => Form is not null;

    public bool IsEmptyResult => Rows.Count == 0;

    /// <summary>Чи є у формі зміни, яких немає в БД.</summary>
    public bool IsDirty { get; private set; }

    protected abstract IReadOnlyList<TEntity> LoadItems();

    protected abstract ReferenceRowViewModel CreateRow(TEntity item);

    protected abstract bool Matches(TEntity item, string search);

    protected abstract TEntity CreateEmpty();

    protected abstract ReferenceFormViewModel<TEntity> CreateForm(TEntity item, Action changed);

    /// <summary>Зберігає позицію через CatalogService; повертає її ідентифікатор.</summary>
    protected abstract int Persist(TEntity item);

    protected abstract void Remove(int id);

    /// <summary>Перечитує позиції з БД і показує позицію selectId, а якщо її немає — першу видиму.</summary>
    protected void Reload(int? selectId = null)
    {
        var items = LoadItems();
        _items = items.ToDictionary(item => item.Id);
        _allRows = items.Select(CreateRow).ToList();
        ApplyFilter();

        if ((_allRows.FirstOrDefault(row => row.Id == selectId) ?? Rows.FirstOrDefault()) is { } row)
        {
            ShowExisting(row);
        }
        else
        {
            MarkSelected(null);
            IsNew = false;
            Form = null;
            IsDirty = false;
        }
    }

    /// <summary>Натискання на рядок. Повторне натискання на вибраний рядок не скидає введеного.</summary>
    protected void SelectRow(ReferenceRowViewModel row)
    {
        if ((row != SelectedRow || IsNew) && ConfirmDiscard())
        {
            ShowExisting(row);
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void NewItem()
    {
        if (!ConfirmDiscard())
        {
            return;
        }

        MarkSelected(null);
        IsNew = true;
        FormTitle = _newItemTitle;
        ShowForm(CreateEmpty());
    }

    [RelayCommand]
    private void Save()
    {
        if (Form is not { } form)
        {
            return;
        }

        if (form.HasInvalidNumbers)
        {
            _dialogs.ShowError("Виправте числа, позначені червоною рамкою.", "Не збережено");
            return;
        }

        try
        {
            Reload(Persist(form.Entity));
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message, "Не збережено");
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedRow is not { } row
            || !_dialogs.Confirm($"Видалити «{row.Title}»? Цю дію не можна скасувати.", _deleteTitle, "Видалити"))
        {
            return;
        }

        try
        {
            Remove(row.Id);
            Reload();
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message, "Не видалено");
        }
    }

    private void ShowExisting(ReferenceRowViewModel row)
    {
        MarkSelected(row);
        IsNew = false;
        FormTitle = row.Title;
        ShowForm(_items[row.Id]);
    }

    /// <summary>Заповнення полів під час створення форми ще не є змінами користувача.</summary>
    private void ShowForm(TEntity item)
    {
        Form = CreateForm(item, () => IsDirty = true);
        IsDirty = false;
    }

    private bool ConfirmDiscard() =>
        !IsDirty || _dialogs.Confirm("Незбережені зміни буде втрачено.", "Відкинути зміни?", "Відкинути", "Лишитися");

    /// <summary>Пошук звужує лише список: форма лишається на вибраній позиції, навіть якщо рядок сховано.</summary>
    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        Rows = search.Length == 0 ? _allRows : _allRows.Where(row => Matches(_items[row.Id], search)).ToList();
    }

    private void MarkSelected(ReferenceRowViewModel? selected)
    {
        foreach (var row in _allRows)
        {
            row.IsSelected = row == selected;
        }

        SelectedRow = selected;
    }
}
