using BrewLog.UI.Features.Common;

namespace BrewLog.UI.Services;

/// <summary>Єдиний спосіб показати повідомлення чи підтвердження: стилізований MessageDialog замість системного MessageBox.</summary>
public sealed class DialogService
{
    public void ShowInfo(string message, string title = "Інформація") =>
        Show(new MessageDialogViewModel(MessageKind.Info, title, message, "Зрозуміло"));

    public void ShowError(string message, string title = "Помилка") =>
        Show(new MessageDialogViewModel(MessageKind.Error, title, message, "Закрити"));

    /// <summary>Питає підтвердження; true — лише якщо користувач явно погодився.</summary>
    public bool Confirm(string message, string title = "Підтвердження", string confirmText = "Підтвердити", string cancelText = "Скасувати") =>
        Show(new MessageDialogViewModel(MessageKind.Confirm, title, message, confirmText, cancelText));

    private static bool Show(MessageDialogViewModel viewModel) =>
        ModalWindow.Show(new MessageDialog(viewModel)) == true;
}
