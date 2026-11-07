using BrewLog.Domain.Common;
using BrewLog.UI.Features.Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrewLog.UI.Features.References;

/// <summary>
/// Форма позиції довідника. Редагує власну копію, яку передає нащадок: у БД вона потрапляє лише після «Зберегти»,
/// а відкинуті зміни не лишають слідів у списку.
/// </summary>
public abstract class ReferenceFormViewModel<TEntity> : ObservableObject
    where TEntity : class, IEntity
{
    private readonly Action _changed;
    private readonly List<NumberField> _numberFields = [];

    /// <param name="entity">Копія, яку редагує форма.</param>
    /// <param name="changed">Повідомляє секцію про зміну будь-якого поля.</param>
    protected ReferenceFormViewModel(TEntity entity, Action changed)
    {
        Entity = entity;
        _changed = changed;
    }

    public TEntity Entity { get; }

    /// <summary>Неправильне число не потрапляє в копію, тому збереження має спершу перевірити це.</summary>
    public bool HasInvalidNumbers => _numberFields.Any(number => !number.IsValid);

    protected NumberField Field(double initial, Action<double> apply, Func<double, string>? format = null)
    {
        var field = new NumberField(initial, apply, _changed, format);
        _numberFields.Add(field);
        return field;
    }

    protected void MarkChanged() => _changed();
}
