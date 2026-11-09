namespace BrewLog.UI.Features.BatchCard;

/// <summary>Крок шкали етапів партії.</summary>
public sealed record StageStepViewModel(int Number, string Title, string DateText, bool IsPassed, bool IsCurrent, bool HasLeftLine, bool HasRightLine);
