using BrewLog.Domain.Common;

namespace BrewLog.Domain.Inventory;

/// <summary>Постачальник сировини.</summary>
public sealed class Supplier : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }
}
