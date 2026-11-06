using BrewLog.Data.Infrastructure;
using BrewLog.Data.Rows;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Recipes;

namespace BrewLog.Data.Repositories;

/// <summary>Довідник інгредієнтів у таблиці ingredient_catalog; кожен рядок стає об'єктом свого класу через фабрику.</summary>
public sealed class IngredientCatalogRepository : Repository<Ingredient>, IIngredientCatalogRepository
{
    public IngredientCatalogRepository(SqliteConnectionFactory connections) : base(connections)
    {
    }

    protected override string TableName => "ingredient_catalog";

    public override IReadOnlyList<Ingredient> GetAll() => Load("SELECT * FROM ingredient_catalog ORDER BY kind, name");

    public IReadOnlyList<Ingredient> GetByKind(IngredientKind kind) =>
        Load("SELECT * FROM ingredient_catalog WHERE kind = @kind ORDER BY name", new { kind = kind.ToString() });

    /// <summary>Порівнює в C#: COLLATE NOCASE у SQLite ігнорує регістр лише для латиниці, а назви бувають кириличні.</summary>
    public Ingredient? FindByName(IngredientKind kind, string name) =>
        GetByKind(kind).FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));

    public override int Insert(Ingredient entity)
    {
        var row = IngredientCatalogRow.FromData(entity.ToData());
        entity.Id = QuerySingle<int>($"{IngredientCatalogRow.InsertSql} SELECT last_insert_rowid();", row);
        entity.CatalogId = entity.Id;
        return entity.Id;
    }

    public override void Update(Ingredient entity) =>
        Execute(IngredientCatalogRow.UpdateSql, IngredientCatalogRow.FromData(entity.ToData()));

    public bool IsUsed(int id) =>
        Exists("SELECT 1 FROM recipe_ingredients WHERE catalog_id = @id UNION ALL SELECT 1 FROM stock_lots WHERE catalog_id = @id", new { id });

    public IReadOnlyDictionary<int, decimal> GetLastPrices() =>
        Query<IngredientCatalogRow>("SELECT id, last_price FROM ingredient_catalog")
            .ToDictionary(row => row.Id, row => (decimal)row.LastPrice);

    protected override IReadOnlyList<Ingredient> Load(string sql, object? parameters = null) =>
        Query<IngredientCatalogRow>(sql, parameters).Select(row => IngredientFactory.FromData(row.ToData())).ToList();
}
