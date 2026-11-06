using BrewLog.Domain.Users;
using BrewLog.UI.Features.Common;
using BrewLog.UI.Shell;

namespace BrewLog.UI.Services;

/// <summary>Знаходить сторінку для розділу меню. Яку розмітку показати, вирішують DataTemplate в App.xaml.</summary>
public sealed class NavigationService
{
    private readonly IReadOnlyDictionary<AppSection, Func<object>> _pageFactories;

    /// <param name="pageFactories">Фабрики ViewModel сторінок за розділами; заповнюються в AppServices.</param>
    public NavigationService(IReadOnlyDictionary<AppSection, Func<object>> pageFactories)
    {
        _pageFactories = pageFactories;
    }

    /// <summary>Створює ViewModel сторінки розділу; якщо сторінки ще немає, повертає заглушку.</summary>
    public object Navigate(AppSection section) =>
        _pageFactories.TryGetValue(section, out var createPage)
            ? createPage()
            : new PlaceholderViewModel(SectionCatalog.Get(section));
}
