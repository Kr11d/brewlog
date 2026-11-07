using BrewLog.Domain.Recipes;

namespace BrewLog.Domain.Abstractions;

/// <summary>Сховище довідника інгредієнтів. Позиція довідника — це Ingredient з Amount = 0 і CatalogId = Id.</summary>
public interface IIngredientCatalogRepository : IRepository<Ingredient>
{
    IReadOnlyList<Ingredient> GetByKind(IngredientKind kind);

    /// <summary>Шукає позицію за назвою без урахування регістру, зокрема для кирилиці.</summary>
    Ingredient? FindByName(IngredientKind kind, string name);

    /// <summary>Чи згадується позиція в рецептах або на складі.</summary>
    bool IsUsed(int id);

    /// <summary>Остання ціна за одиницю, грн, для кожної позиції довідника за її ідентифікатором.</summary>
    IReadOnlyDictionary<int, decimal> GetLastPrices();

    /// <summary>Мінімальний запас в одиницях складу для кожної позиції довідника за її ідентифікатором.</summary>
    IReadOnlyDictionary<int, double> GetMinStocks();
}
