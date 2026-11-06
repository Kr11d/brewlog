using BrewLog.Domain.Users;

namespace BrewLog.Domain.Abstractions;

/// <summary>Поточний сеанс: хто увійшов, що йому дозволено й які розділи меню він бачить.</summary>
public interface ICurrentSession
{
    User? User { get; }

    bool IsAuthenticated { get; }

    IReadOnlyList<AppSection> Sections { get; }

    bool Can(Permission permission);

    /// <summary>Спрацьовує під час входу й виходу, щоб оболонка перебудувала меню.</summary>
    event EventHandler? Changed;
}
