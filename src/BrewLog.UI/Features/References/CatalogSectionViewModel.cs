using BrewLog.Domain.Recipes;
using BrewLog.Domain.Services;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;

namespace BrewLog.UI.Features.References;

/// <summary>
/// Інгредієнти одного типу на вкладці «Інгредієнти». Секція тримає власний вибір і форму, тому перемикання типу нічого не скидає.
/// Клас форми секція не обирає: його створює передана функція. Ціни й мінімуми лише показуються.
/// </summary>
public sealed class CatalogSectionViewModel : ReferenceSectionViewModel<Ingredient>
{
    private readonly CatalogService _catalog;
    private readonly Func<Ingredient, Action, IngredientFormViewModel> _createForm;
    private readonly Action<CatalogSectionViewModel> _selected;

    private IReadOnlyDictionary<int, decimal> _lastPrices = new Dictionary<int, decimal>();
    private IReadOnlyDictionary<int, double> _minStocks = new Dictionary<int, double>();
    private bool _isSelected;

    /// <param name="title">Напис на сегменті.</param>
    /// <param name="newItemTitle">Заголовок форми нової позиції, наприклад «Новий солод».</param>
    /// <param name="createForm">Створює форму свого типу для позиції.</param>
    /// <param name="selected">Повідомляє діалог, що користувач обрав цей тип.</param>
    public CatalogSectionViewModel(
        string title,
        string newItemTitle,
        IngredientKind kind,
        CatalogService catalog,
        DialogService dialogs,
        Func<Ingredient, Action, IngredientFormViewModel> createForm,
        Action<CatalogSectionViewModel> selected)
        : base(dialogs, newItemTitle, "Видалити інгредієнт", "Пошук за назвою")
    {
        Title = title;
        Kind = kind;
        _catalog = catalog;
        _createForm = createForm;
        _selected = selected;
        Reload();
    }

    public string Title { get; }

    public IngredientKind Kind { get; }

    /// <summary>Перемикає RadioButton, тому вибрано завжди лише один тип.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value) && value)
            {
                _selected(this);
            }
        }
    }

    protected override IReadOnlyList<Ingredient> LoadItems()
    {
        _lastPrices = _catalog.GetLastPrices();
        _minStocks = _catalog.GetMinStocks();
        return _catalog.GetCatalog(Kind);
    }

    protected override ReferenceRowViewModel CreateRow(Ingredient item) =>
        new(item.Id, item.Name, $"{DescribePrice(item)} · мінімум {DescribeMinStock(item)}", string.Empty, SelectRow);

    protected override bool Matches(Ingredient item, string search) =>
        item.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase);

    protected override Ingredient CreateEmpty() => IngredientFactory.Create(Kind);

    protected override ReferenceFormViewModel<Ingredient> CreateForm(Ingredient item, Action changed)
    {
        var form = _createForm(item, changed);
        form.ShowStock(DescribePrice(item), DescribeMinStock(item));
        return form;
    }

    protected override int Persist(Ingredient item) => _catalog.SaveCatalogItem(item);

    protected override void Remove(int id) => _catalog.DeleteCatalogItem(id);

    private string DescribePrice(Ingredient item) =>
        $"{DisplayFormat.Money(_lastPrices.GetValueOrDefault(item.Id))}/{item.Unit}";

    private string DescribeMinStock(Ingredient item) =>
        DisplayFormat.Amount(_minStocks.GetValueOrDefault(item.Id), item.Unit);
}
