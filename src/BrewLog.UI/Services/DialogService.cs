using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using BrewLog.UI.Features.Common;
using Microsoft.Win32;

namespace BrewLog.UI.Services;

/// <summary>Єдиний спосіб показати повідомлення, підтвердження чи вибір файлу: стилізований MessageDialog замість системного MessageBox.</summary>
public sealed class DialogService
{
    public void ShowInfo(string message, string title = "Інформація") =>
        Show(new MessageDialogViewModel(MessageKind.Info, title, message, "Зрозуміло"));

    public void ShowError(string message, string title = "Помилка") =>
        Show(new MessageDialogViewModel(MessageKind.Error, title, message, "Закрити"));

    /// <summary>Питає підтвердження; true — лише якщо користувач явно погодився.</summary>
    public bool Confirm(string message, string title = "Підтвердження", string confirmText = "Підтвердити", string cancelText = "Скасувати") =>
        Show(new MessageDialogViewModel(MessageKind.Confirm, title, message, confirmText, cancelText));

    /// <summary>Системний вибір файлу для читання; null — користувач передумав.</summary>
    /// <param name="filter">Фільтр у форматі WPF, наприклад «BeerXML (*.xml)|*.xml».</param>
    public string? PickFileToOpen(string filter, string title)
    {
        var dialog = new OpenFileDialog { Filter = filter, Title = title };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>Системний вибір файлу для запису; null — користувач передумав.</summary>
    /// <param name="filter">Фільтр у форматі WPF, наприклад «BeerXML (*.xml)|*.xml».</param>
    /// <param name="suggestedName">Запропонована назва; символи, заборонені в імені файлу, замінюються.</param>
    public string? PickFileToSave(string filter, string title, string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            Title = title,
            FileName = string.Join("_", suggestedName.Split(Path.GetInvalidFileNameChars())),
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <summary>Записує файл у фоні й відкриває його; помилку запису показує користувачу.</summary>
    public async Task SaveAndOpenAsync(Action save, string path)
    {
        try
        {
            await Task.Run(save);
            OpenFile(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            ShowError($"Не вдалося записати файл «{Path.GetFileName(path)}». Перевірте, що він не відкритий в іншій програмі й що є дозвіл на запис.");
        }
    }

    /// <summary>Відкриває файл програмою, яка призначена для нього в системі; якщо такої немає, повідомляє, що файл збережено.</summary>
    public void OpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            ShowInfo($"Файл збережено, але відкрити його не вдалося: {path}");
        }
    }

    private static bool Show(MessageDialogViewModel viewModel) =>
        ModalWindow.Show(new MessageDialog(viewModel)) == true;
}
