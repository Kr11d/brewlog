using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Users;

namespace BrewLog.Tests.Fakes;

/// <summary>Сесія для тестів сервісів: користувач і дозволи задаються явно, незалежно від ролі.</summary>
public sealed class FakeSession : ICurrentSession
{
    private readonly HashSet<Permission> _permissions;

    public FakeSession(User? user, params Permission[] permissions)
    {
        User = user;
        _permissions = [.. permissions];
    }

    public User? User { get; }

    public bool IsAuthenticated => User is not null;

    public IReadOnlyList<AppSection> Sections => User?.Sections ?? [];

    public bool Can(Permission permission) => _permissions.Contains(permission);

    public event EventHandler? Changed
    {
        add { }
        remove { }
    }
}
