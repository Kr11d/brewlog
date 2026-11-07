using System.Security.Cryptography;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Common;
using BrewLog.Domain.Users;

namespace BrewLog.Domain.Services;

/// <summary>
/// Вхід за логіном і паролем та «Запам'ятати мене». Повертає користувача, а до сесії його додає інтерфейс.
/// Пароль і токен зберігаються лише як хеші.
/// </summary>
public sealed class AuthService
{
    private const int RememberTokenBytes = 32;

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public AuthService(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    /// <summary>
    /// Перевіряє логін і пароль. Для невідомого логіна й невірного пароля повідомлення однакове,
    /// щоб не підказувати, які логіни існують. Про вимкнений запис кажемо лише після правильного пароля.
    /// </summary>
    public User Login(string login, string password)
    {
        var user = _users.FindByLogin(login.Trim());
        if (user is null || !_hasher.Verify(password, user.PasswordHash))
        {
            throw new DomainException("Невірний логін або пароль");
        }

        if (!user.IsActive)
        {
            throw new DomainException("Обліковий запис вимкнено");
        }

        return user;
    }

    /// <summary>Видає новий випадковий токен (32 байти, base64) для «Запам'ятати мене»; у БД потрапляє лише його хеш.</summary>
    public string IssueRememberToken(User user)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(RememberTokenBytes));
        var tokenHash = _hasher.Hash(token);

        _users.UpdateRememberToken(user.Id, tokenHash);
        user.RememberTokenHash = tokenHash;
        return token;
    }

    /// <summary>Вхід за збереженим токеном; null, якщо токен недійсний, відкликаний або запис вимкнено.</summary>
    public User? LoginWithToken(int userId, string token)
    {
        var user = _users.GetById(userId);
        var isValid = user is { IsActive: true, RememberTokenHash: not null }
            && _hasher.Verify(token, user.RememberTokenHash);

        return isValid ? user : null;
    }

    /// <summary>Відкликає токен «Запам'ятати мене»: після виходу автоматичний вхід уже не спрацює.</summary>
    public void RevokeRememberToken(User user)
    {
        _users.UpdateRememberToken(user.Id, null);
        user.RememberTokenHash = null;
    }
}
