using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;

namespace BrewLog.Tests.Fakes;

/// <summary>Сховище в пам'яті для тестів сервісів: основа заглушок конкретних репозиторіїв.</summary>
public class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly List<T> _items = [];
    private int _nextId = 1;

    public T? GetById(int id) => _items.FirstOrDefault(item => item.Id == id);

    public IReadOnlyList<T> GetAll() => _items.ToList();

    public int Insert(T entity)
    {
        entity.Id = _nextId++;
        _items.Add(entity);
        return entity.Id;
    }

    public void Update(T entity) => _items[_items.FindIndex(item => item.Id == entity.Id)] = entity;

    public void Delete(int id) => _items.RemoveAll(item => item.Id == id);
}
