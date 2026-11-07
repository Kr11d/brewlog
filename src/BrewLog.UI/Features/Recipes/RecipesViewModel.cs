using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.Domain.Users;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Recipes;

/// <summary>
/// Сторінка «Рецепти»: таблиця з пошуком і фільтром стилю, прев'ю вибраного рецепта й дії з ним.
/// Показники рахує RecipeService; дії без дозволу EditRecipes приховані.
/// </summary>
public sealed partial class RecipesViewModel : ObservableObject
{
    private static readonly StyleFilterOption AllStyles = new(null, "Усі стилі");

    private readonly RecipeService _recipes;
    private readonly CatalogService _catalog;
    private readonly DialogService _dialogs;
    private readonly WindowService _windows;

    private List<RecipeRowViewModel> _allRows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmpty), nameof(HasRows), nameof(HasNoRecipes), nameof(EmptyTitle), nameof(EmptyHint))]
    private IReadOnlyList<RecipeRowViewModel> _rows = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelection))]
    private RecipeRowViewModel? _selectedRow;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<StyleFilterOption> _styleFilters = [AllStyles];

    /// <summary>Nullable, бо ComboBox на мить скидає вибір, коли змінюється список стилів.</summary>
    [ObservableProperty]
    private StyleFilterOption? _selectedStyleFilter = AllStyles;

    [ObservableProperty]
    private string _subtitle = string.Empty;

    public RecipesViewModel(RecipeService recipes, CatalogService catalog, ICurrentSession session, DialogService dialogs, WindowService windows)
    {
        _recipes = recipes;
        _catalog = catalog;
        _dialogs = dialogs;
        _windows = windows;

        CanEditRecipes = session.Can(Permission.EditRecipes);
        CanEditReferences = session.Can(Permission.EditReferences);
        Reload();
    }

    public bool CanEditRecipes { get; }

    public bool CanEditReferences { get; }

    public bool HasSelection => SelectedRow is not null;

    public bool HasRows => Rows.Count > 0;

    public bool IsEmpty => Rows.Count == 0;

    /// <summary>Порожній стан відрізняє «рецептів немає взагалі» від «фільтр нічого не знайшов».</summary>
    public bool HasNoRecipes => _allRows.Count == 0;

    public string EmptyTitle => HasNoRecipes ? "Рецептів ще немає" : "Нічого не знайдено";

    public string EmptyHint => HasNoRecipes
        ? "Створіть перший рецепт або імпортуйте його з BeerXML."
        : "Змініть пошук або оберіть інший стиль.";

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedStyleFilterChanged(StyleFilterOption? value) => ApplyFilter();

    [RelayCommand]
    private void NewRecipe()
    {
        if (CanEditRecipes && _windows.ShowRecipeEditor(null))
        {
            Reload();
        }
    }

    [RelayCommand]
    private void Edit()
    {
        if (CanEditRecipes && SelectedRow is { } row && _windows.ShowRecipeEditor(row.Id))
        {
            Reload(row.Id);
        }
    }

    [RelayCommand]
    private void Duplicate()
    {
        if (SelectedRow is not { } row)
        {
            return;
        }

        try
        {
            var copy = _recipes.Duplicate(row.Id);
            Reload(copy.Id);
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedRow is not { } row
            || !_dialogs.Confirm($"Видалити рецепт «{row.Name}»? Цю дію не можна скасувати.", "Видалити рецепт", "Видалити"))
        {
            return;
        }

        try
        {
            _recipes.Delete(row.Id);
            Reload();
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }
    }

    [RelayCommand]
    private void ImportBeerXml() => _dialogs.ShowInfo("Буде в B10", "Імпорт BeerXML");

    [RelayCommand]
    private void ExportBeerXml() => _dialogs.ShowInfo("Буде в B10", "Експорт BeerXML");

    [RelayCommand]
    private void OpenReferences() => _dialogs.ShowInfo("Буде в B11", "Довідники");

    /// <summary>Перечитує рецепти й показники; вибраним лишається рецепт selectId, якщо він є.</summary>
    private void Reload(int? selectId = null)
    {
        selectId ??= SelectedRow?.Id;
        var styles = _catalog.GetStyles().ToDictionary(style => style.Id);

        _allRows = _recipes.GetAll()
            .Select(recipe => new RecipeRowViewModel(
                recipe,
                recipe.StyleId is { } styleId ? styles.GetValueOrDefault(styleId) : null,
                _recipes.Calculate(recipe),
                _recipes.EstimateCostPerLiter(recipe)))
            .ToList();
        Subtitle = DisplayFormat.Count(_allRows.Count, "рецепт", "рецепти", "рецептів");

        var filters = _allRows
            .Where(row => row.Style is not null)
            .Select(row => row.Style!)
            .DistinctBy(style => style.Id)
            .OrderBy(style => style.Code, StringComparer.Ordinal)
            .Select(style => new StyleFilterOption(style.Id, $"{style.Code} {style.Name}"))
            .Prepend(AllStyles)
            .ToList();
        StyleFilters = filters;
        SelectedStyleFilter = filters.FirstOrDefault(option => option.StyleId == SelectedStyleFilter?.StyleId) ?? AllStyles;

        ApplyFilter(selectId);
    }

    /// <summary>Застосовує пошук і фільтр; вибір лишається на тому самому рецепті, якщо він видимий, інакше переходить на перший.</summary>
    private void ApplyFilter(int? selectId = null)
    {
        selectId ??= SelectedRow?.Id;
        var search = SearchText.Trim();
        Rows = _allRows.Where(row => row.Matches(search, SelectedStyleFilter?.StyleId)).ToList();
        SelectedRow = Rows.FirstOrDefault(row => row.Id == selectId) ?? Rows.FirstOrDefault();
    }
}
