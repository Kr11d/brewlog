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
}
