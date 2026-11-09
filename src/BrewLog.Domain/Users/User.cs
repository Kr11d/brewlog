using BrewLog.Domain.Common;

namespace BrewLog.Domain.Users;

/// <summary>
/// Користувач застосунку (ієрархія ЛР 1). Роль — це клас: Brewer, Assistant чи Owner сам визначає свої дозволи
/// й розділи меню, тому решта коду лише питає Can(...) і Sections, не перевіряючи роль.
/// </summary>
public abstract class User : IEntity
{
    private const int MinLoginLength = 3;
    private const int MaxLoginLength = 32;

    public int Id { get; set; }

    public string Login { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>Хеш пароля; сам пароль ніде не зберігається.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Хеш токена «Запам'ятати мене»; null — токена немає.</summary>
    public string? RememberTokenHash { get; set; }

    public DateTime CreatedAt { get; set; }

    public abstract UserRole Role { get; }

    /// <summary>Назва ролі для інтерфейсу: «Пивовар», «Помічник», «Власник».</summary>
    public abstract string RoleDisplayName { get; }

    public abstract IReadOnlySet<Permission> Permissions { get; }

    /// <summary>Розділи бічного меню в порядку показу.</summary>
    public abstract IReadOnlyList<AppSection> Sections { get; }

    /// <summary>Розділ, який відкривається після входу. Типово — перший пункт меню.</summary>
    public virtual AppSection HomeSection => Sections[0];

    public bool Can(Permission permission) => Permissions.Contains(permission);

    /// <summary>Чи має користувач доступ до партії з цим виконавцем. Типово — до будь-якої.</summary>
    public virtual bool HasAccessToBatchOf(int? assistantId) => true;

    /// <summary>Перевіряє логін та ім'я. Повертає список помилок українською; порожній список означає, що дані коректні.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        var isLatinOrDigits = Login.All(char.IsAsciiLetterOrDigit);
        if (Login.Length is < MinLoginLength or > MaxLoginLength || !isLatinOrDigits)
        {
            errors.Add($"Логін має містити від {MinLoginLength} до {MaxLoginLength} латинських літер або цифр.");
        }

        if (string.IsNullOrWhiteSpace(FullName))
        {
            errors.Add("Вкажіть ім'я користувача.");
        }

        return errors;
    }

    /// <summary>Незалежна копія: сервіс змінює її й зберігає лише тоді, коли всі правила виконано.</summary>
    public User Clone() => WithRole(Role);

    /// <summary>
    /// Повертає копію облікового запису з указаною роллю. Роль задає клас об'єкта,
    /// тому змінити її «на місці» не можна: фабрика створює об'єкт потрібного класу, а дані переносяться.
    /// </summary>
    public User WithRole(UserRole role)
    {
        var copy = UserFactory.Create(role);
        copy.Id = Id;
        copy.Login = Login;
        copy.FullName = FullName;
        copy.PasswordHash = PasswordHash;
        copy.IsActive = IsActive;
        copy.RememberTokenHash = RememberTokenHash;
        copy.CreatedAt = CreatedAt;
        return copy;
    }
}
