using System.Globalization;
using BrewLog.Domain.Common;

namespace BrewLog.Domain.Recipes;

/// <summary>
/// Інгредієнт рецепта (ієрархія ЛР 1). Кожен тип сам вирішує, що він додає до показників рецепта,
/// тому в розрахунках немає перевірок типу: лише виклик ApplyTo (динамічний поліморфізм).
/// </summary>
public abstract class Ingredient : IEntity
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("uk-UA");

    public int Id { get; set; }

    public int CatalogId { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Кількість в одиницях Unit.</summary>
    public double Amount { get; set; }

    public abstract IngredientKind Kind { get; }

    /// <summary>Одиниця кількості; ціна на складі теж рахується за неї.</summary>
    public abstract string Unit { get; }

    /// <summary>Назва для повідомлень про помилки: рядок без назви теж треба якось позначити.</summary>
    protected string Label => string.IsNullOrWhiteSpace(Name) ? "Інгредієнт" : Name;

    /// <summary>Додає внесок цього рядка до накопичувача показників.</summary>
    public abstract void ApplyTo(RecipeStats stats);

    /// <summary>
    /// Текст внеску рядка для редактора за вже порахованими показниками.
    /// Типово це приріст густини: «+0,031 OG»; типи з іншим внеском перевизначають метод.
    /// </summary>
    public virtual string DescribeContribution(RecipeStats final) =>
        $"{FormatNumber(final.ExtractPointsOf(this) / 1000, "+0.000;-0.000")} OG";

    public virtual IngredientData ToData() => new(Kind)
    {
        Id = Id,
        CatalogId = CatalogId,
        Name = Name,
        Amount = Amount,
    };

    public abstract Ingredient Clone();

    /// <summary>Перевіряє рядок рецепта. Повертає список помилок українською; порожній список означає, що рядок коректний.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>(ValidateCatalogItem());
        if (!(Amount > 0))
        {
            errors.Add($"{Label}: кількість має бути більшою за 0.");
        }

        return errors;
    }

    /// <summary>Перевіряє позицію довідника: там немає кількості (Amount = 0), тому важливі лише назва й характеристики.</summary>
    public IReadOnlyList<string> ValidateCatalogItem()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add("Вкажіть назву інгредієнта.");
        }

        errors.AddRange(ValidateCharacteristics());
        return errors;
    }

    /// <summary>Перевіряє характеристики, властиві саме цьому типу.</summary>
    protected abstract IEnumerable<string> ValidateCharacteristics();

    /// <summary>Форматує число з десятковою комою незалежно від культури системи: текст внеску завжди український.</summary>
    protected static string FormatNumber(double value, string format) => value.ToString(format, DisplayCulture);
}
