using System.Collections.ObjectModel;
using BrewLog.Domain.Recipes;
using BrewLog.UI.Features.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>
/// Секція редактора для одного типу інгредієнтів: рядки рецепта й спливний вибір позиції довідника з пошуком.
/// Клас рядка секція не обирає: його створює передана функція, тож перевірок типу тут немає.
/// </summary>
public sealed partial class IngredientSectionViewModel : ObservableObject
{
    private readonly IReadOnlyList<CatalogOptionViewModel> _allOptions;
    private readonly Func<Ingredient, IngredientLineViewModel> _addToRecipe;
    private readonly Action<Ingredient> _removeFromRecipe;
    private readonly Action _changed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoOptions))]
    private IReadOnlyList<CatalogOptionViewModel> _options = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isPickerOpen;

    /// <param name="title">Заголовок картки.</param>
    /// <param name="catalog">Позиції довідника цього типу.</param>
    /// <param name="addToRecipe">Додає в рецепт копію позиції довідника й повертає її рядок.</param>
    /// <param name="removeFromRecipe">Прибирає інгредієнт із рецепта.</param>
    /// <param name="changed">Повідомляє редактор про зміну складу.</param>
    public IngredientSectionViewModel(
        string title,
        IEnumerable<Ingredient> catalog,
        Func<Ingredient, IngredientLineViewModel> addToRecipe,
        Action<Ingredient> removeFromRecipe,
        Action changed)
    {
        Title = title;
        _allOptions = catalog.Select(item => new CatalogOptionViewModel(item, Add)).ToList();
        Options = _allOptions;
        _addToRecipe = addToRecipe;
        _removeFromRecipe = removeFromRecipe;
        _changed = changed;

        Lines.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasLines));
            OnPropertyChanged(nameof(IsEmpty));
        };
    }

    public string Title { get; }

    public ObservableCollection<IngredientLineViewModel> Lines { get; } = [];

    public bool HasLines => Lines.Count > 0;

    public bool IsEmpty => Lines.Count == 0;

    public bool HasNoOptions => Options.Count == 0;

    /// <summary>Усі числові поля секції: збереження перевіряє, чи немає серед них неправильних.</summary>
    public IEnumerable<NumberField> Fields => Lines.SelectMany(line => line.Fields);

    /// <summary>Показує рядок, який уже є в рецепті.</summary>
    public void Attach(IngredientLineViewModel line)
    {
        line.RemoveRequested += Remove;
        Lines.Add(line);
    }

    partial void OnSearchTextChanged(string value)
    {
        var search = value.Trim();
        Options = _allOptions.Where(option => option.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)).ToList();
    }

    [RelayCommand]
    private void OpenPicker()
    {
        SearchText = string.Empty;
        IsPickerOpen = true;
    }

    [RelayCommand]
    private void ClosePicker() => IsPickerOpen = false;

    /// <summary>Enter у полі пошуку: додає першу знайдену позицію.</summary>
    [RelayCommand]
    private void AddFirstMatch()
    {
        if (Options.Count > 0)
        {
            Add(Options[0].Item);
        }
    }

    private void Add(Ingredient item)
    {
        Attach(_addToRecipe(item));
        IsPickerOpen = false;
        _changed();
    }

    private void Remove(IngredientLineViewModel line)
    {
        line.RemoveRequested -= Remove;
        _removeFromRecipe(line.Ingredient);
        Lines.Remove(line);
        _changed();
    }
}
