using BrewLog.Domain.Common;
using BrewLog.Domain.Inventory;
using BrewLog.Domain.Services;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Inventory;

/// <summary>Діалог замовлення постачальнику: вибір постачальника й попередній підсумок перед збереженням PDF.</summary>
public sealed partial class PurchaseOrderViewModel : ObservableObject
{
    private readonly InventoryService _inventory;
    private readonly DialogService _dialogs;

    [ObservableProperty]
    private SupplierOption? _selectedSupplier;

    [ObservableProperty]
    private string _previewText = string.Empty;

    public PurchaseOrderViewModel(InventoryService inventory, DialogService dialogs)
    {
        _inventory = inventory;
        _dialogs = dialogs;

        Suppliers = inventory.GetSuppliers()
            .Select(supplier => new SupplierOption(supplier.Id, supplier.Name))
            .Append(new SupplierOption(null, "Не вказано"))
            .ToList();
        SelectedSupplier = Suppliers[0];
    }

    /// <summary>Просить вікно закритися; true — замовлення підтверджено.</summary>
    public event Action<bool>? CloseRequested;

    public IReadOnlyList<SupplierOption> Suppliers { get; }

    /// <summary>Замовлення для вибраного постачальника.</summary>
    public PurchaseOrder? Order { get; private set; }

    partial void OnSelectedSupplierChanged(SupplierOption? value)
    {
        try
        {
            Order = _inventory.BuildPurchaseOrder(value?.Id);
            PreviewText = Order.Lines.Count == 0
                ? "Докуповувати нічого: запасів вистачає."
                : $"{DisplayFormat.Count(Order.Lines.Count, "позиція", "позиції", "позицій")}, ≈ {DisplayFormat.Money(Order.Total)}";
        }
        catch (DomainException exception)
        {
            Order = null;
            PreviewText = string.Empty;
            _dialogs.ShowError(exception.Message);
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        if (Order is not { Lines.Count: > 0 })
        {
            _dialogs.ShowInfo("Докуповувати нічого: запасів вистачає.");
            return;
        }

        CloseRequested?.Invoke(true);
    }
}
