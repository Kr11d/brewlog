using BrewLog.Domain.Inventory;
using BrewLog.UI.Formatting;

namespace BrewLog.UI.Features.Batches;

/// <summary>Текст підтвердження запуску варки: спільний для дошки й картки партії.</summary>
public static class WriteOffConfirmation
{
    public static string Describe(string batchNumber, IEnumerable<PlannedWriteOff> lines)
    {
        var items = lines.Select(line => $"{line.Name}: {DisplayFormat.Amount(line.Quantity, line.Unit)}");
        return $"Партію {batchNumber} буде запущено у варку. Зі складу буде списано:{Environment.NewLine}{string.Join(Environment.NewLine, items)}";
    }
}
