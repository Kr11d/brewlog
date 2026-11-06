using System.Data;

namespace BrewLog.Data.Seed;

/// <summary>Наповнює нову БД демо-даними. Залежності сідер отримує через конструктор.</summary>
public interface ISeeder
{
    /// <summary>Порядок запуску: менше значення виконується раніше.</summary>
    int Order { get; }

    /// <param name="today">Дата, від якої рахуються демо-дати, щоб графіки завжди були «свіжі».</param>
    void Seed(IDbConnection c, IDbTransaction tx, DateTime today);
}
