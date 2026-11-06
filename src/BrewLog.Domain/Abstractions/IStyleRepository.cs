using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Abstractions;

/// <summary>Сховище стилів BJCP.</summary>
public interface IStyleRepository : IRepository<BeerStyle>
{
    /// <summary>Чи є рецепти цього стилю.</summary>
    bool IsUsed(int id);
}
