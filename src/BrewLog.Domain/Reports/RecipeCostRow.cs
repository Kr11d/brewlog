namespace BrewLog.Domain.Reports;

/// <summary>Собівартість літра за рецептом серед його розлитих партій, грн/л.</summary>
public sealed record RecipeCostRow(string RecipeName, int BatchCount, decimal AverageCostPerLiter, decimal MinCostPerLiter, decimal MaxCostPerLiter);
