using BrewLog.Domain.Recipes;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Позиція довідника у спливному виборі: натискання додає її в рецепт.</summary>
public sealed partial class CatalogOptionViewModel
{
    private readonly Action<Ingredient> _add;

    public CatalogOptionViewModel(Ingredient item, Action<Ingredient> add)
    {
        Item = item;
        _add = add;
    }

    public Ingredient Item { get; }

    public string Name => Item.Name;

    [RelayCommand]
    private void Add() => _add(Item);
}
