using BrewLog.Domain.Common;

namespace BrewLog.Domain.Users;

/// <summary>
/// Користувач застосунку; роль сама визначає дозволи й розділи меню.
/// Мінімальна версія: ієрархію доробляє розробник B у B5.
/// </summary>
public abstract class User : IEntity
{
    public int Id { get; set; }

    public string Login { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public abstract IReadOnlyList<AppSection> Sections { get; }

    public abstract bool Can(Permission permission);
}
