using BrewLog.Domain.Common;

namespace BrewLog.Domain.Inventory;

/// <summary>Партія сировини на складі: скільки отримано, скільки лишилось, за якою ціною й до якого строку.</summary>
public sealed class StockLot : IEntity
{
    /// <summary>Допуск порівняння кількостей: сума дробів у double відрізняється від точного значення на мізерну величину.</summary>
    public const double QuantityTolerance = 1e-9;

    public int Id { get; set; }

    public int CatalogId { get; init; }

    public int? SupplierId { get; init; }

    public double QuantityReceived { get; init; }

    public double QuantityLeft { get; init; }

    /// <summary>Ціна за одиницю складу, грн.</summary>
    public decimal UnitPrice { get; init; }

    public DateTime ReceivedAt { get; init; }

    public DateTime? ExpiresAt { get; init; }

    public string? LotCode { get; init; }

    /// <summary>Строк рахується днями: партія придатна до кінця дати на упаковці.</summary>
    public bool IsExpired(DateTime now) => ExpiresAt?.Date < now.Date;

    /// <summary>Чи спливає строк упродовж указаної кількості днів; уже прострочена партія сюди не входить.</summary>
    public bool ExpiresWithin(DateTime now, int days) => !IsExpired(now) && ExpiresAt?.Date <= now.Date.AddDays(days);

    /// <summary>Середньозважена ціна за залишками; null, якщо залишків немає.</summary>
    public static decimal? AveragePrice(IEnumerable<StockLot> lots)
    {
        var remaining = lots.Where(lot => lot.QuantityLeft > 0).ToList();
        if (remaining.Count == 0)
        {
            return null;
        }

        return remaining.Sum(lot => (decimal)lot.QuantityLeft * lot.UnitPrice) / remaining.Sum(lot => (decimal)lot.QuantityLeft);
    }

    /// <summary>Перевіряє нову партію. Повертає список помилок українською; порожній список означає, що партія коректна.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (!(QuantityReceived > 0) || !NumberLimits.IsAllowed(QuantityReceived))
        {
            errors.Add("Кількість має бути більшою за 0.");
        }

        if (UnitPrice < 0 || !NumberLimits.IsAllowed((double)UnitPrice))
        {
            errors.Add("Вкажіть коректну ціну за одиницю.");
        }

        if (ExpiresAt?.Date < ReceivedAt.Date)
        {
            errors.Add("Строк придатності не може закінчуватися раніше за дату приходу.");
        }

        return errors;
    }
}
