using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Users;

namespace BrewLog.UI.Services;

/// <summary>Заглушка сесії до промту B5: усе дозволено, видно всі розділи, конкретного користувача немає.</summary>
public sealed class AllowAllSession : ICurrentSession
{
    public User? User => null;

    public bool IsAuthenticated => true;

    public IReadOnlyList<AppSection> Sections { get; } = Enum.GetValues<AppSection>();

    public bool Can(Permission permission) => true;

    /// <summary>Сесія не змінюється, тому подія ніколи не спрацьовує.</summary>
    public event EventHandler? Changed
    {
        add { }
        remove { }
    }
}
