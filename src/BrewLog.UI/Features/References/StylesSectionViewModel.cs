using BrewLog.Domain.Recipes;
using BrewLog.Domain.Services;
using BrewLog.UI.Services;

namespace BrewLog.UI.Features.References;

/// <summary>Вкладка «Стилі BJCP»: стиль, що використовується в рецептах, CatalogService видалити не дасть.</summary>
public sealed class StylesSectionViewModel : ReferenceSectionViewModel<BeerStyle>
{
    private readonly CatalogService _catalog;

    public StylesSectionViewModel(CatalogService catalog, DialogService dialogs)
        : base(dialogs, "Новий стиль", "Видалити стиль", "Пошук за кодом, назвою чи категорією")
    {
        _catalog = catalog;
        Reload();
    }

    protected override IReadOnlyList<BeerStyle> LoadItems() => _catalog.GetStyles();

    protected override ReferenceRowViewModel CreateRow(BeerStyle item) => new(item.Id, item.Name, item.Category, item.Code, SelectRow);

    protected override bool Matches(BeerStyle item, string search) =>
        item.Code.Contains(search, StringComparison.CurrentCultureIgnoreCase)
        || item.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
        || item.Category.Contains(search, StringComparison.CurrentCultureIgnoreCase);

    protected override BeerStyle CreateEmpty() => new();

    protected override ReferenceFormViewModel<BeerStyle> CreateForm(BeerStyle item, Action changed) => new StyleFormViewModel(item, changed);

    protected override int Persist(BeerStyle item) => _catalog.SaveStyle(item);

    protected override void Remove(int id) => _catalog.DeleteStyle(id);
}
