using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Common;

/// <summary>Вміст стилізованого повідомлення: значок, заголовок, текст і одна або дві кнопки.</summary>
public sealed partial class MessageDialogViewModel : ObservableObject
{
    public MessageDialogViewModel(MessageKind kind, string title, string message, string confirmText, string? cancelText = null)
    {
        Kind = kind;
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
    }

    /// <summary>Просить вікно закритися; true — користувач погодився.</summary>
    public event Action<bool>? CloseRequested;

    public MessageKind Kind { get; }

    public string Title { get; }

    public string Message { get; }

    public string ConfirmText { get; }

    public string? CancelText { get; }

    public bool HasCancel => CancelText is not null;

    /// <summary>Коли кнопка одна, Esc теж закриває повідомлення.</summary>
    public bool IsSingleButton => !HasCancel;

    [RelayCommand]
    private void Confirm() => CloseRequested?.Invoke(true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
