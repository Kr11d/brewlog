using BrewLog.Domain.Common;

namespace BrewLog.Domain.Abstractions;

/// <summary>Базові операції зі сховищем сутностей. Реалізація — у BrewLog.Data.</summary>
public interface IRepository<T> where T : class, IEntity
{
    T? GetById(int id);

    IReadOnlyList<T> GetAll();

    /// <summary>Додає сутність і повертає її новий ідентифікатор.</summary>
    int Insert(T entity);

    void Update(T entity);

    void Delete(int id);
}
