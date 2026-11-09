using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Inventory;
using BrewLog.Domain.Services;
using BrewLog.UI.Features.Common;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Batches;

/// <summary>Діалог планування партії. Показує нестачу сировини одразу, але запланувати партію можна й за нестачі.</summary>
public sealed partial class NewBatchViewModel : ObservableObject
{
    private readonly BatchService _batches;
    private readonly InventoryService _inventory;
    private readonly DialogService _dialogs;

    private double _volumeL;

    [ObservableProperty]
    private RecipeOption? _selectedRecipe;

    [ObservableProperty]
    private NumberField _volumeField;

    [ObservableProperty]
    private DateTime? _plannedDate;

    [ObservableProperty]
    private AssistantOption? _selectedAssistant;

    [ObservableProperty]
    private bool _isStockSufficient;

    [ObservableProperty]
    private string _shortageText = string.Empty;

    public NewBatchViewModel(
        RecipeService recipes,
        BatchService batches,
        InventoryService inventory,
        UserService users,
        IClock clock,
        DialogService dialogs)
    {
        _batches = batches;
        _inventory = inventory;
        _dialogs = dialogs;

        Recipes = recipes.GetAll()
            .Select(recipe =>
            {
                var stats = recipes.Calculate(recipe);
                return new RecipeOption(recipe, $"{recipe.Name} · OG {DisplayFormat.Gravity(stats.Og)} · ABV {DisplayFormat.Abv(stats.Abv)}");
            })
            .ToList();
        Assistants = users.GetAssistants()
            .Select(user => new AssistantOption(user.Id, user.FullName))
            .Prepend(new AssistantOption(null, "Без виконавця"))
            .ToList();

        _volumeField = new NumberField(0, _ => { }, () => { });
        _plannedDate = clock.Now.Date;
        _selectedAssistant = Assistants[0];
        SelectedRecipe = Recipes.FirstOrDefault();
    }

    /// <summary>Просить вікно закритися; true — партію заплановано.</summary>
    public event Action<bool>? CloseRequested;

    public IReadOnlyList<RecipeOption> Recipes { get; }

    public IReadOnlyList<AssistantOption> Assistants { get; }

    /// <summary>Обсяг нової партії типово дорівнює обсягу вибраного рецепта.</summary>
    partial void OnSelectedRecipeChanged(RecipeOption? value)
    {
        _volumeL = value?.Recipe.BatchVolumeL ?? 0;
        VolumeField = new NumberField(_volumeL, volume => _volumeL = volume, RefreshAvailability);
        RefreshAvailability();
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedRecipe is not { } option || !HasValidVolume() || PlannedDate is not { } date)
        {
            _dialogs.ShowError("Оберіть рецепт, вкажіть обсяг більший за 0 і заплановану дату.");
            return;
        }

        try
        {
            _batches.Plan(option.Recipe.Id, _volumeL, date, SelectedAssistant?.Id);
            CloseRequested?.Invoke(true);
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }
    }

    private bool HasValidVolume() => VolumeField.IsValid && _volumeL > 0;

    /// <summary>Поки рецепт чи обсяг неправильні, результат перевірки не показується.</summary>
    private void RefreshAvailability()
    {
        if (SelectedRecipe is not { } option || !HasValidVolume())
        {
            IsStockSufficient = false;
            ShortageText = string.Empty;
            return;
        }

        var result = _inventory.CheckAvailability(option.Recipe, _volumeL);
        IsStockSufficient = result.IsSufficient;
        ShortageText = result.IsSufficient ? string.Empty : Shortage.DescribeAll(result.Shortages);
    }
}
