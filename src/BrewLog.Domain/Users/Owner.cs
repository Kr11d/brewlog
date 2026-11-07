using System.Collections.Frozen;

namespace BrewLog.Domain.Users;

/// <summary>Власник: склад, звіти й облікові записи; партії лише переглядає.</summary>
public sealed class Owner : User
{
    private static readonly FrozenSet<Permission> RolePermissions = new[]
    {
        Permission.ViewBatches,
        Permission.ViewStock,
        Permission.ManageStock,
        Permission.ViewReports,
        Permission.ManageUsers,
    }.ToFrozenSet();

    private static readonly IReadOnlyList<AppSection> RoleSections =
        [AppSection.Batches, AppSection.Inventory, AppSection.Reports, AppSection.Users];

    public override UserRole Role => UserRole.Owner;

    public override string RoleDisplayName => "Власник";

    public override IReadOnlySet<Permission> Permissions => RolePermissions;

    public override IReadOnlyList<AppSection> Sections => RoleSections;
}
