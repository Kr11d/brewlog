using BrewLog.Domain.Users;

namespace BrewLog.Tests.Users;

public sealed class RoleTests
{
    /// <summary>Матриця дозволів — рядок за рядком із HANDOFF §3.2.</summary>
    [Theory]
    [InlineData(UserRole.Brewer, Permission.ViewRecipes, true)]
    [InlineData(UserRole.Brewer, Permission.EditRecipes, true)]
    [InlineData(UserRole.Brewer, Permission.EditReferences, true)]
    [InlineData(UserRole.Brewer, Permission.ViewBatches, true)]
    [InlineData(UserRole.Brewer, Permission.ManageBatches, true)]
    [InlineData(UserRole.Brewer, Permission.AddMeasurements, true)]
    [InlineData(UserRole.Brewer, Permission.PackageBatches, true)]
    [InlineData(UserRole.Brewer, Permission.ViewStock, true)]
    [InlineData(UserRole.Brewer, Permission.ManageStock, false)]
    [InlineData(UserRole.Brewer, Permission.ViewReports, false)]
    [InlineData(UserRole.Brewer, Permission.ManageUsers, false)]
    [InlineData(UserRole.Assistant, Permission.ViewRecipes, false)]
    [InlineData(UserRole.Assistant, Permission.EditRecipes, false)]
    [InlineData(UserRole.Assistant, Permission.EditReferences, false)]
    [InlineData(UserRole.Assistant, Permission.ViewBatches, true)]
    [InlineData(UserRole.Assistant, Permission.ManageBatches, false)]
    [InlineData(UserRole.Assistant, Permission.AddMeasurements, true)]
    [InlineData(UserRole.Assistant, Permission.PackageBatches, true)]
    [InlineData(UserRole.Assistant, Permission.ViewStock, false)]
    [InlineData(UserRole.Assistant, Permission.ManageStock, false)]
    [InlineData(UserRole.Assistant, Permission.ViewReports, false)]
    [InlineData(UserRole.Assistant, Permission.ManageUsers, false)]
    [InlineData(UserRole.Owner, Permission.ViewRecipes, false)]
    [InlineData(UserRole.Owner, Permission.EditRecipes, false)]
    [InlineData(UserRole.Owner, Permission.EditReferences, false)]
    [InlineData(UserRole.Owner, Permission.ViewBatches, true)]
    [InlineData(UserRole.Owner, Permission.ManageBatches, false)]
    [InlineData(UserRole.Owner, Permission.AddMeasurements, false)]
    [InlineData(UserRole.Owner, Permission.PackageBatches, false)]
    [InlineData(UserRole.Owner, Permission.ViewStock, true)]
    [InlineData(UserRole.Owner, Permission.ManageStock, true)]
    [InlineData(UserRole.Owner, Permission.ViewReports, true)]
    [InlineData(UserRole.Owner, Permission.ManageUsers, true)]
    public void Can_RoleAndPermission_MatchesHandoffMatrix(UserRole role, Permission permission, bool expected)
    {
        Assert.Equal(expected, UserFactory.Create(role).Can(permission));
    }

    /// <summary>Розділи й домашній розділ — HANDOFF §3.1.</summary>
    [Theory]
    [InlineData(UserRole.Brewer, AppSection.Recipes, new[] { AppSection.Recipes, AppSection.Batches, AppSection.Inventory })]
    [InlineData(UserRole.Assistant, AppSection.MyBatches, new[] { AppSection.MyBatches, AppSection.Measurements, AppSection.Bottling })]
    [InlineData(UserRole.Owner, AppSection.Batches, new[] { AppSection.Batches, AppSection.Inventory, AppSection.Reports, AppSection.Users })]
    public void Sections_Role_FollowHandoffOrderAndHomeSection(UserRole role, AppSection home, AppSection[] expected)
    {
        var user = UserFactory.Create(role);

        Assert.Equal(expected, user.Sections);
        Assert.Equal(home, user.HomeSection);
    }

    [Theory]
    [InlineData(UserRole.Brewer, typeof(Brewer))]
    [InlineData(UserRole.Assistant, typeof(Assistant))]
    [InlineData(UserRole.Owner, typeof(Owner))]
    public void Create_Role_ReturnsMatchingClass(UserRole role, Type expectedType)
    {
        var user = UserFactory.Create(role);

        Assert.IsType(expectedType, user);
        Assert.Equal(role, user.Role);
    }
}
