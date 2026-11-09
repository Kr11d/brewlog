namespace BrewLog.Domain.Batches;

/// <summary>Попередження про перебіг бродіння з текстом для користувача.</summary>
public sealed record FermentationAlert(AlertKind Kind, Severity Severity, string Message);
