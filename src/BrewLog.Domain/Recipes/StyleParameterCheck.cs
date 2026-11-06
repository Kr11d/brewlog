using BrewLog.Domain.Common;

namespace BrewLog.Domain.Recipes;

/// <summary>Результат перевірки одного показника: значення, діапазон стилю, статус і позиція 0..1 для шкали.</summary>
public sealed record StyleParameterCheck(
    string Parameter,
    double Value,
    ValueRange Range,
    StyleCheckStatus Status,
    double Position);
