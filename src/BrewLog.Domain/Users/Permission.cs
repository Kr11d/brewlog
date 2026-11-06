namespace BrewLog.Domain.Users;

/// <summary>Дозволи на дії в застосунку. Матриця за ролями — HANDOFF §3.2.</summary>
public enum Permission
{
    ViewRecipes,
    EditRecipes,
    EditReferences,
    ViewBatches,
    ManageBatches,
    AddMeasurements,
    PackageBatches,
    ViewStock,
    ManageStock,
    ViewReports,
    ManageUsers,
}
