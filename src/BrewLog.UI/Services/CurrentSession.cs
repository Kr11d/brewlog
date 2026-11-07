using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Users;

namespace BrewLog.UI.Services;

/// <summary>
/// Сесія застосунку: хто увійшов. Дозволи й розділи меню визначає клас ролі користувача, сесія лише питає його.
/// Без входу нічого не дозволено й меню порожнє.
/// </summary>
public sealed class CurrentSession : ICurrentSession
{
    public User? User { get; private set; }

    public bool IsAuthenticated => User is not null;

    public IReadOnlyList<AppSection> Sections => User?.Sections ?? [];

    public bool Can(Permission permission) => User?.Can(permission) ?? false;

    public event EventHandler? Changed;

    /// <summary>Робить користувача поточним; автентифікацію вже виконав AuthService.</summary>
    public void SignIn(User user)
    {
        User = user;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SignOut()
    {
        User = null;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
