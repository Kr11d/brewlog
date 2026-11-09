using BrewLog.Domain.Common;

namespace BrewLog.Domain.Inventory;

/// <summary>Сировини бракує для списання. Повідомлення перелічує нестачі українською, а Shortages дає їх окремими записами.</summary>
public sealed class InsufficientStockException : DomainException
{
    public InsufficientStockException(IReadOnlyList<Shortage> shortages) : base(Shortage.DescribeAll(shortages))
    {
        Shortages = shortages;
    }

    public IReadOnlyList<Shortage> Shortages { get; }
}
