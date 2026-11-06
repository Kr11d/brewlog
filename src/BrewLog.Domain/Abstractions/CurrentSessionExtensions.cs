using BrewLog.Domain.Common;
using BrewLog.Domain.Users;

namespace BrewLog.Domain.Abstractions;

/// <summary>Спільна перевірка дозволів для сервісів.</summary>
public static class CurrentSessionExtensions
{
    /// <summary>Кидає DomainException, якщо в поточного користувача немає дозволу.</summary>
    public static void Require(this ICurrentSession session, Permission permission)
    {
        if (!session.Can(permission))
        {
            throw new DomainException("Недостатньо прав для цієї дії.");
        }
    }
}
