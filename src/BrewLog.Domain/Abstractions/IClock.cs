namespace BrewLog.Domain.Abstractions;

/// <summary>Джерело поточного часу: сервіси беруть його замість системного годинника, а тести підставляють фіксовану дату.</summary>
public interface IClock
{
    DateTime Now { get; }
}
