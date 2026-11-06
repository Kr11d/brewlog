using BrewLog.Data.Infrastructure;
using BrewLog.Data.Rows;
using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Recipes;
using Dapper;

namespace BrewLog.Data.Repositories;

/// <summary>Стилі BJCP у таблиці styles.</summary>
public sealed class StyleRepository : Repository<BeerStyle>, IStyleRepository
{
    public StyleRepository(SqliteConnectionFactory connections) : base(connections)
    {
    }

    protected override string TableName => "styles";

    public override IReadOnlyList<BeerStyle> GetAll() => Load("SELECT * FROM styles ORDER BY code");

    public override int Insert(BeerStyle entity)
    {
        entity.Id = QuerySingle<int>($"{StyleRow.InsertSql} SELECT last_insert_rowid();", StyleRow.FromStyle(entity));
        return entity.Id;
    }

    public override void Update(BeerStyle entity) => Execute(StyleRow.UpdateSql, StyleRow.FromStyle(entity));

    public bool IsUsed(int id) => Exists("SELECT 1 FROM recipes WHERE style_id = @id", new { id });

    protected override IReadOnlyList<BeerStyle> Load(string sql, object? parameters = null) =>
        Query<StyleRow>(sql, parameters).Select(row => row.ToStyle()).ToList();
}
