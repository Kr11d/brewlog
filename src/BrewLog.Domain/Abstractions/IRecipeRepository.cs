using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Abstractions;

/// <summary>Сховище рецептів разом із їхніми рядками.</summary>
public interface IRecipeRepository : IRepository<Recipe>
{
    /// <summary>Чи є партії за цим рецептом.</summary>
    bool IsUsedInBatches(int id);
}
