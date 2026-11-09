namespace BrewLog.UI.Features.Batches;

/// <summary>Колонка дошки: один етап, кількість його партій і картки для показу.</summary>
public sealed record BoardColumnViewModel(string StateCode, string Title, int Count, IReadOnlyList<BatchItemViewModel> Items);
