using System.Data;
using BrewLog.Data.Rows;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Users;
using Dapper;

namespace BrewLog.Data.Seed;

/// <summary>Три демо-облікові записи, по одному на роль (HANDOFF §3.3). У БД потрапляють лише хеші паролів.</summary>
public sealed class UserSeeder : ISeeder
{
    private readonly IPasswordHasher _hasher;

    public UserSeeder(IPasswordHasher hasher)
    {
        _hasher = hasher;
    }

    public int Order => 10;

    public void Seed(IDbConnection c, IDbTransaction tx, DateTime today)
    {
        var createdAt = today.AddMonths(-9);

        UserRow Account(UserRole role, string login, string password, string fullName)
        {
            var user = UserFactory.Create(role);
            user.Login = login;
            user.FullName = fullName;
            user.PasswordHash = _hasher.Hash(password);
            user.CreatedAt = createdAt;
            return UserRow.FromUser(user);
        }

        c.Execute(
            UserRow.InsertSql,
            new[]
            {
                Account(UserRole.Brewer, "brewer", "brewer123", "Андрій Солодовник"),
                Account(UserRole.Assistant, "assistant", "assistant123", "Марта Хмелевська"),
                Account(UserRole.Owner, "owner", "owner123", "Ірина Броварська"),
            },
            tx);
    }
}
