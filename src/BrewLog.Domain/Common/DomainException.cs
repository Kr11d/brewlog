namespace BrewLog.Domain.Common;

/// <summary>Порушення бізнес-правила або нестача дозволу. Повідомлення українською — його бачить користувач.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Кидає виняток зі списком помилок перевірки, якщо вони є: кожна помилка з нового рядка.</summary>
    public static void ThrowIfAny(IReadOnlyCollection<string> errors)
    {
        if (errors.Count > 0)
        {
            throw new DomainException(string.Join(Environment.NewLine, errors));
        }
    }
}
