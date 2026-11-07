using BrewLog.Domain.Users;

namespace BrewLog.UI.Features.Users;

/// <summary>Пункт випадного списку ролей: значення й українська назва, яку дає сам клас ролі.</summary>
public sealed record RoleOption(UserRole Role, string DisplayName);
