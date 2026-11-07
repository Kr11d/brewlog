using BrewLog.Domain.Recipes;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrewLog.UI.Features.References;

/// <summary>
/// Форма позиції довідника інгредієнтів: спільне тут, характеристики — у нащадку за типом.
/// Розмітку характеристик WPF обирає за класом нащадка (DataTemplate у ReferencesDialog), тому перевірок типу немає.
/// </summary>
public abstract partial class IngredientFormViewModel : ReferenceFormViewModel<Ingredient>
{
    [ObservableProperty]
    private string _name = string.Empty;

    /// <param name="item">Копія позиції, яку редагує форма.</param>
    protected IngredientFormViewModel(Ingredient item, Action changed) : base(item, changed)
    {
        Name = item.Name;
    }

    /// <summary>Лише для читання: ціни редагує власник у «Складі».</summary>
    public string LastPrice { get; private set; } = string.Empty;

    /// <summary>Лише для читання: мінімуми редагує власник у «Складі».</summary>
    public string MinStock { get; private set; } = string.Empty;

    public void ShowStock(string lastPrice, string minStock)
    {
        LastPrice = lastPrice;
        MinStock = minStock;
    }

    partial void OnNameChanged(string value)
    {
        Entity.Name = value.Trim();
        MarkChanged();
    }
}
