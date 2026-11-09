using System.Globalization;

namespace BrewLog.Domain.Inventory;

/// <summary>Замовлення постачальнику; постачальника може не бути, якщо його не вибрано.</summary>
public sealed record PurchaseOrder(Supplier? Supplier, DateTime CreatedAt, IReadOnlyList<PurchaseOrderLine> Lines)
{
    public decimal Total => Lines.Sum(line => line.Total);

    /// <summary>Номер ЗП-РРРРММДД-N. Замовлення не зберігаються в БД, тому N — хвилина доби: номери за день зростають і не потребують лічильника.</summary>
    public string Number => $"ЗП-{CreatedAt.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}-{CreatedAt.Hour * 60 + CreatedAt.Minute + 1}";
}
