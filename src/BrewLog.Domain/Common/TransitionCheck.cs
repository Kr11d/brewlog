namespace BrewLog.Domain.Common;

/// <summary>Результат перевірки переходу: чи дозволено, а якщо ні — причина для користувача.</summary>
public sealed record TransitionCheck(bool Allowed, string? Reason)
{
    public static TransitionCheck Ok() => new(true, null);

    public static TransitionCheck Deny(string reason) => new(false, reason);
}
