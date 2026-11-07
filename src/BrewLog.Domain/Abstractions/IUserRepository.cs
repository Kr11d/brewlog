using BrewLog.Domain.Users;

namespace BrewLog.Domain.Abstractions;

/// <summary>Сховище облікових записів; кожен запис повертається об'єктом класу своєї ролі.</summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>Шукає обліковий запис за логіном без урахування регістру.</summary>
    User? FindByLogin(string login);

    /// <summary>
    /// Записує лише хеш токена «Запам'ятати мене» (null — прибрати токен). Окремо від Update,
    /// щоб застарілий об'єкт користувача із сесії не перезаписав зміни, зроблені в діалозі «Користувачі».
    /// </summary>
    void UpdateRememberToken(int userId, string? tokenHash);
}
