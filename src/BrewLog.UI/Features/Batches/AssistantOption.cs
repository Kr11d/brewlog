namespace BrewLog.UI.Features.Batches;

/// <summary>Вибір виконавця партії; Id = null означає «без виконавця».</summary>
public sealed record AssistantOption(int? Id, string Title);
