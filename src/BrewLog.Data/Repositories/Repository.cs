using BrewLog.Data.Infrastructure;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using Dapper;

namespace BrewLog.Data.Repositories;

/// <summary>
/// Абстрактний репозиторій на Dapper зі спільним читанням і видаленням.
/// Узагальнений клас — статичний поліморфізм (ЛР 1): той самий код працює з будь-якою сутністю T.
/// </summary>
public abstract class Repository<T> : IRepository<T> where T : class, IEntity
{
    protected Repository(SqliteConnectionFactory connections)
    {
        Connections = connections;
    }

    protected SqliteConnectionFactory Connections { get; }

    protected abstract string TableName { get; }

    public virtual T? GetById(int id) =>
        Load($"SELECT * FROM {TableName} WHERE id = @id", new { id }).FirstOrDefault();

    public virtual IReadOnlyList<T> GetAll() => Load($"SELECT * FROM {TableName}");

    public abstract int Insert(T entity);

    public abstract void Update(T entity);

    public virtual void Delete(int id) => Execute($"DELETE FROM {TableName} WHERE id = @id", new { id });

    /// <summary>
    /// Читає сутності запитом. Типово Dapper створює T напряму; поліморфні сутності перевизначають метод:
    /// читають рядки Rows/*Row і перетворюють їх через фабрику.
    /// </summary>
    protected virtual IReadOnlyList<T> Load(string sql, object? parameters = null) => Query<T>(sql, parameters);

    protected IReadOnlyList<TRow> Query<TRow>(string sql, object? parameters = null)
    {
        using var connection = Connections.Open();
        return connection.Query<TRow>(sql, parameters).AsList();
    }

    protected TRow? QuerySingle<TRow>(string sql, object? parameters = null)
    {
        using var connection = Connections.Open();
        return connection.QueryFirstOrDefault<TRow>(sql, parameters);
    }

    protected int Execute(string sql, object? parameters = null)
    {
        using var connection = Connections.Open();
        return connection.Execute(sql, parameters);
    }

    /// <summary>Чи повертає запит хоча б один рядок.</summary>
    protected bool Exists(string sql, object? parameters = null) =>
        QuerySingle<int>($"SELECT EXISTS ({sql})", parameters) != 0;
}
