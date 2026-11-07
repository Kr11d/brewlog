using System.Collections.Frozen;

namespace BrewLog.Domain.Users;

/// <summary>Помічник: працює з призначеними йому партіями — виміри й розлив; рецептів не бачить.</summary>
public sealed class Assistant : User
{
    private static readonly FrozenSet<Permission> RolePermissions = new[]
    {
        Permission.ViewBatches,
        Permission.AddMeasurements,
        Permission.PackageBatches,
    }.ToFrozenSet();

    private static readonly IReadOnlyList<AppSection> RoleSections = [AppSection.MyBatches, AppSection.Measurements, AppSection.Bottling];

    public override UserRole Role => UserRole.Assistant;

    public override string RoleDisplayName => "Помічник";

    public override IReadOnlySet<Permission> Permissions => RolePermissions;

    public override IReadOnlyList<AppSection> Sections => RoleSections;
}
