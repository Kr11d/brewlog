using BrewLog.UI.Shell;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BrewLog.UI.Features.Common;

/// <summary>Сторінка-заглушка для розділу, який ще не зроблено.</summary>
public sealed class PlaceholderViewModel : ObservableObject
{
    public PlaceholderViewModel(SectionInfo section)
    {
        Title = section.Title;
        Glyph = section.Glyph;
        Description = $"Тут з'явиться розділ «{section.Title}».";
    }

    public string Title { get; }

    public string Glyph { get; }

    public string Description { get; }
}
