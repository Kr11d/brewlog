using BrewLog.UI.Features.Common;

namespace BrewLog.UI.Features.References;

/// <summary>Рядок форми стилю: показник і поля його нижньої та верхньої межі.</summary>
public sealed record StyleRangeFields(string Label, NumberField Min, NumberField Max);
