using System.Data;
using BrewLog.Data.Repositories;
using BrewLog.Domain.Inventory;
using Dapper;

namespace BrewLog.Data.Seed;

/// <summary>
/// Демо-склад (HANDOFF §10). На American IPA 50 л потрібно по 160 г Citra й Mosaic, а є 120 г і 90 г; решти для неї вистачає.
/// Нижче мінімуму ще Vienna, Chocolate і Cascade; строк менше 30 днів — у вівсяних пластівців і Hallertauer;
/// одна партія Nottingham прострочена, але поруч є свіжа. Дати рахуються від today.
/// </summary>
public sealed class StockSeeder : ISeeder
{
    private const string MaltHouse = "ТОВ «Солодова справа»";
    private const string HopYard = "ФОП «Хмільний двір»";

    public int Order => 40;

    public void Seed(IDbConnection c, IDbTransaction tx, DateTime today)
    {
        var catalogIds = c.Query<(int Id, string Name)>("SELECT id, name FROM ingredient_catalog", transaction: tx)
            .ToDictionary(item => item.Name, item => item.Id);
        var supplierIds = c.Query<(int Id, string Name)>("SELECT id, name FROM suppliers", transaction: tx)
            .ToDictionary(item => item.Name, item => item.Id);
        var ownerId = c.QuerySingle<int>("SELECT id FROM users WHERE login = 'owner'", transaction: tx);

        var number = 0;
        foreach (var (name, quantity, price, receivedDaysAgo, expiresInDays, supplier) in Lots())
        {
            var receivedAt = today.AddDays(-receivedDaysAgo);
            var lot = new StockLot
            {
                CatalogId = catalogIds[name],
                SupplierId = supplierIds[supplier],
                QuantityReceived = quantity,
                QuantityLeft = quantity,
                UnitPrice = price,
                ReceivedAt = receivedAt,
                ExpiresAt = expiresInDays is { } days ? today.AddDays(days) : null,
                LotCode = $"L-{receivedAt:yyMM}-{++number:000}",
            };

            StockRepository.InsertReceivedLot(c, tx, lot, ownerId);
        }
    }

    private static IEnumerable<(string Name, double Quantity, decimal Price, int ReceivedDaysAgo, int? ExpiresInDays, string Supplier)> Lots() =>
    [
        ("Pilsner", 60, 36m, 75, 240, MaltHouse),
        ("Pilsner", 40, 38m, 20, 330, MaltHouse),
        ("Pale Ale", 55, 41m, 60, 260, MaltHouse),
        ("Pale Ale", 30, 42m, 15, 340, MaltHouse),
        ("Munich", 18, 48m, 50, 280, MaltHouse),
        ("Vienna", 4, 46m, 90, 200, MaltHouse),
        ("Wheat", 22, 44m, 40, 290, MaltHouse),
        ("Carapils", 9, 62m, 55, 270, MaltHouse),
        ("Crystal 60", 7, 68m, 55, 270, MaltHouse),
        ("Chocolate", 1.5, 78m, 120, 180, MaltHouse),
        ("Roasted Barley", 6, 74m, 45, 300, MaltHouse),
        ("Вівсяні пластівці", 5, 40m, 150, 18, MaltHouse),

        ("Saaz", 900, 1.60m, 70, 400, HopYard),
        ("Hallertauer Mittelfrüh", 450, 1.80m, 340, 25, HopYard),
        ("East Kent Goldings", 500, 2.00m, 80, 380, HopYard),
        ("Magnum", 350, 1.40m, 65, 420, HopYard),
        ("Cascade", 180, 1.90m, 100, 350, HopYard),
        ("Citra", 120, 3.20m, 35, 400, HopYard),
        ("Mosaic", 90, 3.40m, 35, 400, HopYard),

        ("US-05", 12, 165m, 30, 330, HopYard),
        ("S-04", 8, 155m, 30, 330, HopYard),
        ("W-34/70", 6, 210m, 45, 300, HopYard),
        ("WB-06", 5, 160m, 45, 300, HopYard),
        ("Nottingham", 2, 165m, 400, -12, HopYard),
        ("Nottingham", 5, 170m, 25, 340, HopYard),

        ("Сахароза", 12, 32m, 40, null, MaltHouse),
        ("Декстроза", 6, 55m, 40, null, MaltHouse),
    ];
}
