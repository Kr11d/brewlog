namespace BrewLog.UI.Features.Inventory;

/// <summary>Рядок зведеної потреби для запланованих варок.</summary>
public sealed record RequirementRowViewModel(string Name, string RequiredText, string AvailableText, string ShortageText, string BatchesText, bool HasShortage);
