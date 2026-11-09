using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.UI.Features.Common;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Inventory;

/// <summary>Діалог приходу сировини. Постачальника можна вибрати зі списку або надрукувати нового.</summary>
public sealed partial class StockReceiptViewModel : ObservableObject
{
    private readonly InventoryService _inventory;
    private readonly DialogService _dialogs;

    private double _quantity;
    private double _price;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuantityLabel))]
    [NotifyPropertyChangedFor(nameof(PriceLabel))]
    private CatalogItemOption? _selectedItem;

    [ObservableProperty]
    private NumberField _quantityField;

    [ObservableProperty]
    private NumberField _priceField;

    [ObservableProperty]
    private string _supplierText = string.Empty;

    [ObservableProperty]
    private DateTime? _expiresAt;

    [ObservableProperty]
    private string _lotCode = string.Empty;

    public StockReceiptViewModel(InventoryService inventory, IClock clock, DialogService dialogs)
    {
        _inventory = inventory;
        _dialogs = dialogs;

        var now = clock.Now;
        Items = InventoryRows.Build(inventory.GetInventory(now), now, new Dictionary<int, string>())
            .OrderBy(row => row.Name, StringComparer.CurrentCulture)
            .Select(row => new CatalogItemOption(row.CatalogId, $"{row.Name} — {row.KindTitle}", row.Unit))
            .ToList();
        Suppliers = inventory.GetSuppliers().Select(supplier => supplier.Name).ToList();

        _quantityField = new NumberField(0, value => _quantity = value, () => { });
        _priceField = new NumberField(0, value => _price = value, () => { });
    }

    /// <summary>Просить вікно закритися; true — прихід оформлено.</summary>
    public event Action<bool>? CloseRequested;

    public IReadOnlyList<CatalogItemOption> Items { get; }

    public IReadOnlyList<string> Suppliers { get; }

    public string QuantityLabel => SelectedItem is { } item ? $"Кількість, {item.Unit}" : "Кількість";

    public string PriceLabel => SelectedItem is { } item ? $"Ціна за {item.Unit}, грн" : "Ціна за одиницю, грн";

    /// <summary>Підставляє поточну ціну позиції: зазвичай новий прихід коштує близько того самого.</summary>
    partial void OnSelectedItemChanged(CatalogItemOption? value)
    {
        if (value is not null)
        {
            _price = Math.Round((double)_inventory.CurrentUnitPrice(value.CatalogId), 2);
            PriceField = new NumberField(_price, number => _price = number, () => { });
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedItem is not { } item || !QuantityField.IsValid || !PriceField.IsValid)
        {
            _dialogs.ShowError("Оберіть позицію, вкажіть кількість і ціну числами.");
            return;
        }

        try
        {
            var supplierId = string.IsNullOrWhiteSpace(SupplierText) ? (int?)null : _inventory.FindOrAddSupplier(SupplierText).Id;
            _inventory.ReceiveStock(item.CatalogId, _quantity, (decimal)_price, supplierId, ExpiresAt, LotCode);
            CloseRequested?.Invoke(true);
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }
    }
}
