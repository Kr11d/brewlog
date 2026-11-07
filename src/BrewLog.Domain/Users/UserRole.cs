namespace BrewLog.Domain.Users;

/// <summary>Роль користувача. Зберігається в колонці users.role; клас користувача за нею обирає лише UserFactory.</summary>
public enum UserRole
{
    Brewer,
    Assistant,
    Owner,
}
