using BrewLog.Data.Infrastructure;
using BrewLog.Data.Rows;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Users;

namespace BrewLog.Data.Repositories;

/// <summary>Облікові записи в таблиці users; кожен рядок стає об'єктом класу своєї ролі через UserFactory.</summary>
public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(SqliteConnectionFactory connections) : base(connections)
    {
    }

    protected override string TableName => "users";

    public override IReadOnlyList<User> GetAll() => Load("SELECT * FROM users ORDER BY full_name");

    /// <summary>COLLATE NOCASE тут достатньо: логін складається лише з латиниці й цифр.</summary>
    public User? FindByLogin(string login) =>
        Load("SELECT * FROM users WHERE login = @login COLLATE NOCASE", new { login }).FirstOrDefault();

    public override int Insert(User entity)
    {
        entity.Id = QuerySingle<int>($"{UserRow.InsertSql} SELECT last_insert_rowid();", UserRow.FromUser(entity));
        return entity.Id;
    }

    public override void Update(User entity) => Execute(UserRow.UpdateSql, UserRow.FromUser(entity));

    public void UpdateRememberToken(int userId, string? tokenHash) =>
        Execute("UPDATE users SET remember_token_hash = @tokenHash WHERE id = @userId", new { userId, tokenHash });

    protected override IReadOnlyList<User> Load(string sql, object? parameters = null) =>
        Query<UserRow>(sql, parameters).Select(row => row.ToUser()).ToList();
}
