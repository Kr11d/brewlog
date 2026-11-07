using BrewLog.Domain.Common;

namespace BrewLog.Domain.Users;

/// <summary>Фабрика користувачів: єдине місце, де клас обирається за роллю через switch (патерн Factory).</summary>
public static class UserFactory
{
    /// <summary>Створює порожній обліковий запис потрібного класу; дані заповнює той, хто викликав.</summary>
    public static User Create(UserRole role) => role switch
    {
        UserRole.Brewer => new Brewer(),
        UserRole.Assistant => new Assistant(),
        UserRole.Owner => new Owner(),
        _ => throw new DomainException($"Невідома роль користувача: {role}."),
    };
}
