using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Abstractions;

/// <summary>Читає й пише рецепти у форматі BeerXML 1.0. Реалізація — у Data; сервіс бачить лише потоки.</summary>
public interface IBeerXmlSerializer
{
    /// <summary>Читає всі рецепти файлу. Позиції довідника ще не прив'язані: CatalogId рядків дорівнює 0.</summary>
    /// <exception cref="DomainException">Вміст не є коректним BeerXML.</exception>
    IReadOnlyList<Recipe> Import(Stream stream);

    void Export(IEnumerable<Recipe> recipes, Stream stream);
}
