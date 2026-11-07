using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Спливний вибір позиції довідника. Окремий UserControl, щоб smoke міг перевірити розмітку, не відкриваючи поп-ап.</summary>
public partial class CatalogPicker : UserControl
{
    public CatalogPicker()
    {
        InitializeComponent();
        IsVisibleChanged += OnIsVisibleChanged;
    }

    /// <summary>Пошук одразу готовий до введення. Фокус ставиться із затримкою: поки поп-ап відкривається, поле ще не може його прийняти.</summary>
    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, () => SearchInput.Focus());
        }
    }
}
