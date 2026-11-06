using BrewLog.Domain.Users;

namespace BrewLog.UI.Shell;

/// <summary>Як розділ виглядає в меню: підпис і гліф.</summary>
public sealed record SectionInfo(AppSection Section, string Title, string Glyph);
