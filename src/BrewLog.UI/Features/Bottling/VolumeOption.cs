namespace BrewLog.UI.Features.Bottling;

/// <summary>Варіант обсягу пляшки чи кега для сегментованого вибору.</summary>
public sealed record VolumeOption(double Value, string Title, bool IsDefault);
