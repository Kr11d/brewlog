using BrewLog.Domain.Recipes;
using BrewLog.Domain.Services;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrewLog.UI.Features.References;

/// <summary>
/// Діалог «Довідники»: вкладка стилів і вкладка інгредієнтів із секцією на кожен тип.
/// Тип секції задається один раз під час створення, тож перевірок типу інгредієнта тут немає.
/// </summary>
public sealed partial class ReferencesViewModel : ObservableObject
{
    private readonly CatalogService _catalog;
    private readonly DialogService _dialogs;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private CatalogSectionViewModel? _selectedKind;

    public ReferencesViewModel(CatalogService catalog, DialogService dialogs)
    {
        _catalog = catalog;
        _dialogs = dialogs;

        Styles = new StylesSectionViewModel(catalog, dialogs);
        Kinds =
        [
            CreateKind<Malt>("Солод", "Новий солод", IngredientKind.Malt, (malt, changed) => new MaltFormViewModel(malt, changed)),
            CreateKind<Hop>("Хміль", "Новий хміль", IngredientKind.Hop, (hop, changed) => new HopFormViewModel(hop, changed)),
            CreateKind<Yeast>("Дріжджі", "Нові дріжджі", IngredientKind.Yeast, (yeast, changed) => new YeastFormViewModel(yeast, changed)),
            CreateKind<Adjunct>("Додатки", "Новий додаток", IngredientKind.Adjunct, (adjunct, changed) => new AdjunctFormViewModel(adjunct, changed)),
        ];
        Kinds[0].IsSelected = true;
    }

    public StylesSectionViewModel Styles { get; }

    public IReadOnlyList<CatalogSectionViewModel> Kinds { get; }

    /// <summary>Викликається під час закриття вікна будь-яким способом: незбережені зміни потребують підтвердження.</summary>
    public bool ConfirmClose()
    {
        var hasChanges = Styles.IsDirty || Kinds.Any(kind => kind.IsDirty);
        return !hasChanges || _dialogs.Confirm("Незбережені зміни буде втрачено.", "Закрити без збереження?", "Закрити", "Лишитися");
    }

    /// <summary>Узагальнений метод задає тип секції один раз: форма отримує копію позиції свого класу.</summary>
    private CatalogSectionViewModel CreateKind<T>(string title, string newItemTitle, IngredientKind kind, Func<T, Action, IngredientFormViewModel> createForm)
        where T : Ingredient =>
        new(title, newItemTitle, kind, _catalog, _dialogs, (item, changed) => createForm((T)item.Clone(), changed), SelectKind);

    private void SelectKind(CatalogSectionViewModel selected)
    {
        foreach (var kind in Kinds.Where(kind => kind != selected))
        {
            kind.IsSelected = false;
        }

        SelectedKind = selected;
    }
}
