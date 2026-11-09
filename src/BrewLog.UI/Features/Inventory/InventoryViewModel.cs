using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Inventory;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.UI.Features.Common;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Inventory;

/// <summary>
/// Сторінка «Склад»: показники, залишки з фільтрами, партії вибраної позиції, потреба для запланованих варок.
/// Дії й форма редагування потребують ManageStock, тож пивовар бачить сторінку лише для перегляду.
/// </summary>
public sealed partial class InventoryViewModel : ObservableObject
{
    private readonly InventoryService _inventory;
    private readonly BatchService _batches;
    private readonly CatalogService _catalog;
    private readonly IClock _clock;
    private readonly DialogService _dialogs;
    private readonly WindowService _windows;

    private List<InventoryRowViewModel> _allRows = [];
    private IngredientKind? _kindFilter;
    private double _minStock;
    private double _price;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRows))]
    [NotifyPropertyChangedFor(nameof(IsEmpty))]
    private IReadOnlyList<InventoryRowViewModel> _rows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private InventoryRowViewModel? _selectedRow;

    [ObservableProperty]
    private NumberField? _minStockField;

    [ObservableProperty]
    private NumberField? _priceField;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _onlyBelowMinimum;

    [ObservableProperty]
    private bool _onlyExpiring;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    [ObservableProperty]
    private int _belowMinimumCount;

    [ObservableProperty]
    private int _expiringCount;

    [ObservableProperty]
    private int _expiredCount;

    [ObservableProperty]
    private string _stockValueText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRequirements))]
    [NotifyPropertyChangedFor(nameof(HasNoRequirements))]
    private IReadOnlyList<RequirementRowViewModel> _requirements = [];

    public InventoryViewModel(
        InventoryService inventory,
        BatchService batches,
        CatalogService catalog,
        ICurrentSession session,
        IClock clock,
        DialogService dialogs,
        WindowService windows)
    {
        _inventory = inventory;
        _batches = batches;
        _catalog = catalog;
        _clock = clock;
        _dialogs = dialogs;
        _windows = windows;

        CanManageStock = session.Can(Permission.ManageStock);
        Reload();
    }

    public bool CanManageStock { get; }

    public bool HasRows => Rows.Count > 0;

    public bool IsEmpty => Rows.Count == 0;

    public bool HasSelection => SelectedRow is not null;

    public bool HasRequirements => Requirements.Count > 0;

    public bool HasNoRequirements => Requirements.Count == 0;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnOnlyBelowMinimumChanged(bool value) => ApplyFilter();

    partial void OnOnlyExpiringChanged(bool value) => ApplyFilter();

    /// <summary>Заповнює форму значеннями вибраної позиції; вона показує останню ціну, бо редагується саме вона.</summary>
    partial void OnSelectedRowChanged(InventoryRowViewModel? value)
    {
        if (value is null || !CanManageStock)
        {
            return;
        }

        _minStock = value.MinStock;
        _price = (double)_catalog.GetLastPrices().GetValueOrDefault(value.CatalogId);
        MinStockField = new NumberField(_minStock, number => _minStock = number, () => { });
        PriceField = new NumberField(_price, number => _price = number, () => { });
    }

    [RelayCommand]
    private void FilterKind(IngredientKind? kind)
    {
        _kindFilter = kind;
        ApplyFilter();
    }

    [RelayCommand]
    private void ReceiveStock()
    {
        if (CanManageStock && _windows.ShowStockReceipt())
        {
            Reload();
        }
    }

    [RelayCommand]
    private void CreateOrder() => _dialogs.ShowInfo("Буде в A8");

    [RelayCommand]
    private void ExportExcel() => _dialogs.ShowInfo("Буде в A8");

    [RelayCommand]
    private void SaveItem()
    {
        if (SelectedRow is not { } row || MinStockField is not { IsValid: true } || PriceField is not { IsValid: true })
        {
            _dialogs.ShowError("Вкажіть мінімум і ціну числами.");
            return;
        }

        try
        {
            _inventory.UpdateMinStock(row.CatalogId, _minStock);
            _inventory.UpdatePrice(row.CatalogId, (decimal)_price);
            Reload();
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }
    }

    private void Reload()
    {
        var now = _clock.Now;
        var suppliers = _inventory.GetSuppliers().ToDictionary(supplier => supplier.Id, supplier => supplier.Name);

        _allRows = InventoryRows.Build(_inventory.GetInventory(now), now, suppliers)
            .OrderBy(row => row.Name, StringComparer.CurrentCulture)
            .ToList();

        Subtitle = $"Станом на {DisplayFormat.Date(now)}";
        BelowMinimumCount = _allRows.Count(row => row.IsBelowMinimum);
        ExpiringCount = _allRows.Count(row => row.IsExpiring);
        ExpiredCount = _allRows.Count(row => row.IsExpired);
        StockValueText = DisplayFormat.MoneyValue(_allRows.Sum(row => row.Value));
        Requirements = BuildRequirements();

        ApplyFilter();
    }

    /// <summary>Зводить потребу всіх запланованих партій; якщо позиції бракує, називає партії, яким вона потрібна.</summary>
    private List<RequirementRowViewModel> BuildRequirements()
    {
        var rows = _allRows.ToDictionary(row => row.CatalogId);

        return _batches.GetPlanned()
            .SelectMany(batch => _inventory.Requirement(batch.RecipeId, batch.VolumeL).Select(need => (batch.Number, CatalogId: need.Key, Quantity: need.Value)))
            .Where(need => rows.ContainsKey(need.CatalogId))
            .GroupBy(need => need.CatalogId)
            .Select(group =>
            {
                var row = rows[group.Key];
                var required = group.Sum(need => need.Quantity);
                var shortage = required - row.Available;
                var hasShortage = shortage > StockLot.QuantityTolerance;
                return new RequirementRowViewModel(
                    row.Name,
                    DisplayFormat.Amount(required, row.Unit),
                    row.AvailableText,
                    hasShortage ? DisplayFormat.Amount(shortage, row.Unit) : string.Empty,
                    hasShortage ? string.Join(", ", group.Select(need => need.Number).Distinct()) : string.Empty,
                    hasShortage);
            })
            .OrderByDescending(row => row.HasShortage)
            .ThenBy(row => row.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    /// <summary>Застосовує фільтри; вибір лишається на тій самій позиції, якщо вона видима, інакше переходить на першу.</summary>
    private void ApplyFilter()
    {
        var selectedId = SelectedRow?.CatalogId;
        var search = SearchText.Trim();

        Rows = _allRows
            .Where(row => _kindFilter is null || row.Kind == _kindFilter)
            .Where(row => search.Length == 0 || row.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            .Where(row => !OnlyBelowMinimum || row.IsBelowMinimum)
            .Where(row => !OnlyExpiring || row.IsExpiring || row.IsExpired)
            .ToList();
        SelectedRow = Rows.FirstOrDefault(row => row.CatalogId == selectedId) ?? Rows.FirstOrDefault();
    }
}
