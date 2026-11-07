using BrewLog.UI.Formatting;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrewLog.UI.Features.Common;

/// <summary>
/// Числове поле форми, яке оновлює дані під час друку. Тримає текст, а не double: прив'язка до double
/// переписала б «1,» на «1», і десяткову кому не вдалося б надрукувати. Правильне значення одразу йде власнику,
/// неправильне лишає попереднє й позначається червоною рамкою (стиль NumberBox).
/// </summary>
public sealed partial class NumberField : ObservableObject
{
    private readonly Action<double> _apply;
    private readonly Action _changed;
    private string _text;

    [ObservableProperty]
    private bool _isValid = true;

    /// <param name="initial">Початкове значення; виклику apply і changed воно не спричиняє.</param>
    /// <param name="apply">Передає власнику правильне значення.</param>
    /// <param name="changed">Викликається на кожну зміну тексту, навіть неправильну.</param>
    /// <param name="format">Як показати початкове значення; типово DisplayFormat.Number.</param>
    public NumberField(double initial, Action<double> apply, Action changed, Func<double, string>? format = null)
    {
        _text = (format ?? DisplayFormat.Number)(initial);
        _apply = apply;
        _changed = changed;
    }

    public string Text
    {
        get => _text;
        set
        {
            if (!SetProperty(ref _text, value))
            {
                return;
            }

            IsValid = DisplayFormat.TryParseNumber(value, out var number);
            if (IsValid)
            {
                _apply(number);
            }

            _changed();
        }
    }
}
