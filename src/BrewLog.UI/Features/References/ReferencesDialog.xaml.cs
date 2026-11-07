using System.Windows;
using BrewLog.UI.Controls;

namespace BrewLog.UI.Features.References;

/// <summary>Діалог «Довідники». Відкривається лише через WindowService.</summary>
public partial class ReferencesDialog : Window
{
    public ReferencesDialog(ReferencesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Поле пошуку лежить у шаблоні, тому FocusManager.FocusedElement до нього не дістається.
        Loaded += (_, _) => SearchShortcut.FocusTarget(this);

        // Сюди сходяться і Esc, і хрестик заголовка: жоден із них не втратить зміни мовчки.
        Closing += (_, e) => e.Cancel = !viewModel.ConfirmClose();
    }
}
