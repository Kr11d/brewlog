using BrewLog.Domain.Common;
using BrewLog.Domain.Ibu;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Services;
using BrewLog.UI.Features.Common;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>
/// Вікно «Редактор рецепта». Працює з власним екземпляром рецепта: кожна зміна одразу записується в нього й перераховує показники
/// через RecipeService, а в БД потрапляє лише після «Зберегти». Тому скасування нічого не лишає.
/// Склад читає узагальнений IngredientsOf&lt;T&gt;, а клас рядка задається для кожної секції, тож перевірок типу інгредієнта тут немає.
/// </summary>
public sealed partial class RecipeEditorViewModel : ObservableObject
{
    private const double NewRecipeVolumeL = 50;
    private const double StartMaltKg = 1;
    private const double StartHopGrams = 25;
    private const double StartYeastPacks = 1;
    private const double StartAdjunctKg = 0.5;

    private readonly RecipeService _recipes;
    private readonly CatalogService _catalog;
    private readonly DialogService _dialogs;
    private readonly Recipe _recipe;
    private readonly IngredientSectionViewModel[] _sections;
    private readonly NumberField[] _parameterFields;

    /// <summary>Під час заповнення форми зміни полів ще не є змінами користувача.</summary>
    private readonly bool _isLoaded;
    private bool _isSaved;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewName))]
    private string _name = string.Empty;

    [ObservableProperty]
    private StyleOption? _selectedStyle;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private RecipeStatsSummary? _stats;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoStyleRows))]
    private IReadOnlyList<StyleRangeRow> _styleRows = [];

    /// <param name="recipe">Рецепт для редагування, окремий екземпляр із БД; null — новий рецепт.</param>
    public RecipeEditorViewModel(RecipeService recipes, CatalogService catalog, DialogService dialogs, Recipe? recipe)
    {
        _recipes = recipes;
        _catalog = catalog;
        _dialogs = dialogs;
        _recipe = recipe ?? new Recipe { BatchVolumeL = NewRecipeVolumeL };

        Title = recipe is null ? "Новий рецепт" : "Редагування рецепта";

        VolumeL = ParameterField(_recipe.BatchVolumeL, value => _recipe.BatchVolumeL = value);
        Efficiency = ParameterField(_recipe.EfficiencyPercent, value => _recipe.EfficiencyPercent = value);
        BoilTime = ParameterField(_recipe.BoilTimeMin, value => _recipe.BoilTimeMin = (int)Math.Round(value));
        _parameterFields = [VolumeL, Efficiency, BoilTime];

        Formulas = IbuFormulaFactory.All
            .Select(formula => new FormulaOptionViewModel(formula, formula.Code == _recipe.IbuFormulaCode, SelectFormula))
            .ToList();

        var noStyle = new StyleOption(null, "Без стилю");
        Styles = _catalog.GetStyles()
            .OrderBy(style => style.Code, StringComparer.Ordinal)
            .Select(style => new StyleOption(style, $"{style.Code} {style.Name}"))
            .Prepend(noStyle)
            .ToList();

        Malts = CreateSection<Malt>("Солод", IngredientKind.Malt, StartMaltKg, malt => new MaltLineViewModel(malt, MarkChanged));
        Hops = CreateSection<Hop>("Хміль", IngredientKind.Hop, StartHopGrams, hop => new HopLineViewModel(hop, MarkChanged));
        Yeasts = CreateSection<Yeast>("Дріжджі", IngredientKind.Yeast, StartYeastPacks, yeast => new YeastLineViewModel(yeast, MarkChanged));
        Adjuncts = CreateSection<Adjunct>("Додатки", IngredientKind.Adjunct, StartAdjunctKg, adjunct => new AdjunctLineViewModel(adjunct, MarkChanged));
        _sections = [Malts, Hops, Yeasts, Adjuncts];

        Name = _recipe.Name;
        Notes = _recipe.Notes ?? string.Empty;
        SelectedStyle = Styles.FirstOrDefault(option => option.Style?.Id == _recipe.StyleId) ?? noStyle;

        _isLoaded = true;
        Recalculate();
    }

    /// <summary>Просить вікно закритися; true — рецепт збережено.</summary>
    public event Action<bool>? CloseRequested;

    public string Title { get; }

    public string WindowTitle => $"{Title} — BrewLog";

    /// <summary>Назва на «документ-картці»: показує введене одразу, а поки поле порожнє — заголовок вікна.</summary>
    public string PreviewName => string.IsNullOrWhiteSpace(Name) ? Title : Name;

    public NumberField VolumeL { get; }

    public NumberField Efficiency { get; }

    public NumberField BoilTime { get; }

    public IReadOnlyList<FormulaOptionViewModel> Formulas { get; }

    public IReadOnlyList<StyleOption> Styles { get; }

    public IngredientSectionViewModel Malts { get; }

    public IngredientSectionViewModel Hops { get; }

    public IngredientSectionViewModel Yeasts { get; }

    public IngredientSectionViewModel Adjuncts { get; }

    public bool HasNoStyleRows => StyleRows.Count == 0;

    /// <summary>Чи є зміни, яких немає в БД.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>Ідентифікатор збереженого рецепта; 0, поки не збережено.</summary>
    public int SavedId { get; private set; }

    /// <summary>Викликається під час закриття вікна будь-яким способом: незбережені зміни потребують підтвердження.</summary>
    public bool ConfirmClose() =>
        _isSaved || !IsDirty || _dialogs.Confirm("Незбережені зміни буде втрачено.", "Закрити без збереження?", "Закрити", "Лишитися");

    partial void OnNameChanged(string value)
    {
        _recipe.Name = value.Trim();
        MarkChanged();
    }

    partial void OnNotesChanged(string value)
    {
        _recipe.Notes = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        MarkChanged();
    }

    partial void OnSelectedStyleChanged(StyleOption? value)
    {
        _recipe.StyleId = value?.Style?.Id;
        MarkChanged();
    }

    [RelayCommand]
    private void Save()
    {
        if (_parameterFields.Concat(_sections.SelectMany(section => section.Fields)).Any(field => !field.IsValid))
        {
            _dialogs.ShowError("Виправте числа, позначені червоною рамкою.", "Рецепт не збережено");
            return;
        }

        try
        {
            SavedId = _recipes.Save(_recipe);
            _isSaved = true;
            CloseRequested?.Invoke(true);
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message, "Рецепт не збережено");
        }
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    private NumberField ParameterField(double initial, Action<double> apply) => new(initial, apply, MarkChanged);

    /// <summary>Узагальнений метод задає тип секції один раз: він обирає і рядки рецепта, і клас їхніх обгорток.</summary>
    private IngredientSectionViewModel CreateSection<T>(string title, IngredientKind kind, double startAmount, Func<T, IngredientLineViewModel> createLine)
        where T : Ingredient
    {
        var section = new IngredientSectionViewModel(
            title,
            _catalog.GetCatalog(kind),
            item => createLine(AddFromCatalog<T>(item, startAmount)),
            _recipe.RemoveIngredient,
            MarkChanged);

        foreach (var ingredient in _recipe.IngredientsOf<T>())
        {
            section.Attach(createLine(ingredient));
        }

        return section;
    }

    /// <summary>
    /// Рядок рецепта — копія позиції довідника: характеристики далі змінюються лише в цьому рецепті.
    /// Час кипіння потрібен лише хмелю, решта типів це поле ігнорують.
    /// </summary>
    private T AddFromCatalog<T>(Ingredient item, double startAmount)
        where T : Ingredient
    {
        var ingredient = (T)IngredientFactory.FromData(item.ToData() with { Id = 0, Amount = startAmount, TimeMin = _recipe.BoilTimeMin });
        _recipe.AddIngredient(ingredient);
        return ingredient;
    }

    private void SelectFormula(string code)
    {
        _recipe.IbuFormulaCode = code;
        MarkChanged();
    }

    private void MarkChanged()
    {
        if (_isLoaded)
        {
            IsDirty = true;
            Recalculate();
        }
    }

    private void Recalculate()
    {
        var stats = _recipes.Calculate(_recipe);
        Stats = new RecipeStatsSummary(stats, _recipes.EstimateCostPerLiter(_recipe));

        foreach (var line in _sections.SelectMany(section => section.Lines))
        {
            line.Refresh(stats);
        }

        StyleRows = SelectedStyle?.Style is { } style
            ? StyleCheck.Check(style, stats).Select(StyleRangeRow.From).ToList()
            : [];
    }
}
