using System.Collections.Frozen;

namespace BrewLog.Domain.Users;

/// <summary>Пивовар: веде рецепти й довідники, планує партії та переводить їх між етапами, склад лише переглядає.</summary>
public sealed class Brewer : User
{
    private static readonly FrozenSet<Permission> RolePermissions = new[]
    {
        Permission.ViewRecipes,
        Permission.EditRecipes,
        Permission.EditReferences,
        Permission.ViewBatches,
        Permission.ManageBatches,
        Permission.AddMeasurements,
        Permission.PackageBatches,
        Permission.ViewStock,
    }.ToFrozenSet();

    private static readonly IReadOnlyList<AppSection> RoleSections = [AppSection.Recipes, AppSection.Batches, AppSection.Inventory];

    public override UserRole Role => UserRole.Brewer;

    public override string RoleDisplayName => "Пивовар";

    public override IReadOnlySet<Permission> Permissions => RolePermissions;

    public override IReadOnlyList<AppSection> Sections => RoleSections;
}
