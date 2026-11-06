using BrewLog.Domain.Common;

namespace BrewLog.Domain.Ibu;

/// <summary>Фабрика формул IBU: перетворює збережений у рецепті код на об'єкт формули.</summary>
public static class IbuFormulaFactory
{
    /// <summary>Усі доступні формули, наприклад для перемикача в редакторі.</summary>
    public static IReadOnlyList<IbuFormula> All { get; } = [new TinsethFormula(), new RagerFormula()];

    /// <summary>Формула нового рецепта.</summary>
    public static IbuFormula Default => All[0];

    public static IbuFormula FromCode(string code) =>
        All.FirstOrDefault(formula => formula.Code == code)
        ?? throw new DomainException($"Невідома формула IBU: {code}.");
}
