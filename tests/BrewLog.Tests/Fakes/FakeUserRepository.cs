using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Users;

namespace BrewLog.Tests.Fakes;

/// <summary>Заглушка сховища облікових записів: логін шукається без урахування регістру, як у справжньому репозиторії.</summary>
public sealed class FakeUserRepository : InMemoryRepository<User>, IUserRepository
{
    /// <summary>Додає активний обліковий запис; пароль хешується тим самим FakePasswordHasher, що й у сервісах.</summary>
    public User Add(UserRole role, string login, string password = "secret1", bool isActive = true)
    {
        var user = UserFactory.Create(role);
        user.Login = login;
        user.FullName = $"Користувач {login}";
        user.PasswordHash = new FakePasswordHasher().Hash(password);
        user.IsActive = isActive;
        Insert(user);
        return user;
    }

    public User? FindByLogin(string login) =>
        GetAll().FirstOrDefault(user => string.Equals(user.Login, login, StringComparison.OrdinalIgnoreCase));

    public void UpdateRememberToken(int userId, string? tokenHash)
    {
        if (GetById(userId) is { } user)
        {
            user.RememberTokenHash = tokenHash;
        }
    }
}
