using BrewLog.Domain.Recipes;
using BrewLog.UI.Features.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>
/// Рядок таблиці редактора: обгортка над інгредієнтом рецепта. Спільне тут, поля своїх характеристик додає клас кожного типу.
/// Зміна поля одразу записується в інгредієнт, а внесок рядка текст дає сам інгредієнт (DescribeContribution).
/// </summary>
public abstract partial class IngredientLineViewModel : ObservableObject
{
    private readonly Action _changed;
    private readonly List<NumberField> _fields = [];

    [ObservableProperty]
    private string _contribution = string.Empty;

    protected IngredientLineViewModel(Ingredient ingredient, Action changed)
    {
        Ingredient = ingredient;
        _changed = changed;
        Amount = Field(ingredient.Amount, value => ingredient.Amount = value);
    }

    /// <summary>Просить секцію прибрати цей рядок.</summary>
    public event Action<IngredientLineViewModel>? RemoveRequested;

    public Ingredient Ingredient { get; }

    /// <summary>Назва лише для читання: вона пов'язує рядок із позицією довідника.</summary>
    public string Name => Ingredient.Name;

    public NumberField Amount { get; }

    /// <summary>Усі числові поля рядка: збереження перевіряє, чи немає серед них неправильних.</summary>
    public IReadOnlyList<NumberField> Fields => _fields;

    /// <summary>Оновлює текст внеску за щойно порахованими показниками.</summary>
    public void Refresh(RecipeStats stats) => Contribution = Ingredient.DescribeContribution(stats);

    protected NumberField Field(double initial, Action<double> apply)
    {
        var field = new NumberField(initial, apply, _changed);
        _fields.Add(field);
        return field;
    }

    /// <summary>Для змін поза числовими полями, наприклад вибору використання хмелю.</summary>
    protected void NotifyChanged() => _changed();

    [RelayCommand]
    private void Remove() => RemoveRequested?.Invoke(this);
}
