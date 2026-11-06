using BrewLog.Domain.Common;
using BrewLog.Domain.Ibu;

namespace BrewLog.Domain.Recipes;

/// <summary>Рецепт: параметри варки й упорядкований список інгредієнтів. Рядки змінюються лише через методи класу.</summary>
public sealed class Recipe : IEntity
{
    private readonly List<Ingredient> _ingredients = [];

    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? StyleId { get; set; }

    public double BatchVolumeL { get; set; }

    public double EfficiencyPercent { get; set; } = 72;

    public int BoilTimeMin { get; set; } = 60;

    /// <summary>Код формули IBU; об'єкт формули створює IbuFormulaFactory.</summary>
    public string IbuFormulaCode { get; set; } = IbuFormulaFactory.Default.Code;

    public string? Notes { get; set; }

    public int? AuthorId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public IReadOnlyList<Ingredient> Ingredients => _ingredients;

    public void AddIngredient(Ingredient ingredient) => _ingredients.Add(ingredient);

    public void RemoveIngredient(Ingredient ingredient) => _ingredients.Remove(ingredient);

    /// <summary>Переставляє рядок на нову позицію; позиція поза списком обмежується його межами.</summary>
    public void MoveIngredient(Ingredient ingredient, int newIndex)
    {
        if (_ingredients.Remove(ingredient))
        {
            _ingredients.Insert(Math.Clamp(newIndex, 0, _ingredients.Count), ingredient);
        }
    }

    /// <summary>Рядки одного типу в порядку рецепта (статичний поліморфізм, ЛР 1).</summary>
    public IReadOnlyList<T> IngredientsOf<T>() where T : Ingredient => _ingredients.OfType<T>().ToList();

    /// <summary>Створює незалежну копію з новою назвою: рядки клонуються, а ідентифікатори скидаються, щоб зберегти її як новий рецепт.</summary>
    public Recipe CopyAs(string name)
    {
        var copy = new Recipe
        {
            Name = name,
            StyleId = StyleId,
            BatchVolumeL = BatchVolumeL,
            EfficiencyPercent = EfficiencyPercent,
            BoilTimeMin = BoilTimeMin,
            IbuFormulaCode = IbuFormulaCode,
            Notes = Notes,
        };

        foreach (var ingredient in _ingredients)
        {
            var line = ingredient.Clone();
            line.Id = 0;
            copy.AddIngredient(line);
        }

        return copy;
    }

    /// <summary>Масштаб для партії: k = обсяг партії / обсяг рецепта; за нульового обсягу рецепта 0.</summary>
    public double ScaleFactor(double volumeL) => BatchVolumeL > 0 ? volumeL / BatchVolumeL : 0;

    /// <summary>Повертає список помилок українською разом із помилками рядків; порожній список означає, що рецепт коректний.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Вкажіть назву рецепта.");
        }

        if (!(BatchVolumeL > 0))
        {
            errors.Add("Обсяг партії має бути більшим за 0.");
        }

        if (!(EfficiencyPercent is > 0 and <= 100))
        {
            errors.Add("Ефективність має бути більшою за 0 і не більшою за 100 %.");
        }

        if (BoilTimeMin < 0)
        {
            errors.Add("Час кипіння не може бути від'ємним.");
        }

        if (IbuFormulaFactory.All.All(formula => formula.Code != IbuFormulaCode))
        {
            errors.Add("Оберіть формулу IBU.");
        }

        errors.AddRange(_ingredients.SelectMany(ingredient => ingredient.Validate()));
        return errors;
    }
}
