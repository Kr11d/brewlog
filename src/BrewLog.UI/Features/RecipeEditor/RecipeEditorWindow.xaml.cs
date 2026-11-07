using System.Windows;

namespace BrewLog.UI.Features.RecipeEditor;

/// <summary>Вікно редактора рецепта. Відкривається лише через WindowService.</summary>
public partial class RecipeEditorWindow : Window
{
    private readonly RecipeEditorViewModel _viewModel;

    public RecipeEditorWindow(RecipeEditorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = _viewModel = viewModel;
        viewModel.CloseRequested += result => DialogResult = result;

        // Сюди сходяться і «Скасувати», і Esc, і хрестик заголовка: жоден із них не втратить зміни мовчки.
        Closing += (_, e) => e.Cancel = !viewModel.ConfirmClose();
    }

    /// <summary>Ідентифікатор збереженого рецепта; 0, поки не збережено.</summary>
    public int SavedId => _viewModel.SavedId;
}
