using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Ibu;

/// <summary>Формула гіркоти (ієрархія ЛР 1, патерн Strategy): рецепт зберігає код формули, а не її логіку.</summary>
public abstract class IbuFormula
{
    /// <summary>Код для збереження в рецепті; збігається зі значеннями колонки ibu_formula у БД.</summary>
    public abstract string Code { get; }

    /// <summary>Назва для інтерфейсу.</summary>
    public abstract string DisplayName { get; }

    /// <summary>Рахує IBU одного варильного внесення хмелю.</summary>
    /// <param name="hop">Внесення: кількість у грамах, альфа-кислоти, час кипіння.</param>
    /// <param name="boilGravity">Густина кипіння; у рецепті дорівнює OG.</param>
    /// <param name="volumeL">Обсяг партії, л; має бути більшим за 0.</param>
    public abstract double Calculate(Hop hop, double boilGravity, double volumeL);
}
