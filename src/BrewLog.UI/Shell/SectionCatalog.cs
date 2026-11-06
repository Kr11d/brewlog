using BrewLog.Domain.Users;

namespace BrewLog.UI.Shell;

/// <summary>Підписи й іконки всіх розділів в одному місці; гліфи — зі шрифту Segoe Fluent Icons.</summary>
public static class SectionCatalog
{
    private static readonly IReadOnlyDictionary<AppSection, SectionInfo> Sections =
        new SectionInfo[]
        {
            new(AppSection.Recipes, "Рецепти", ""),
            new(AppSection.Batches, "Партії", ""),
            new(AppSection.MyBatches, "Мої партії", ""),
            new(AppSection.Measurements, "Журнал вимірів", ""),
            new(AppSection.Bottling, "Розлив", ""),
            new(AppSection.Inventory, "Склад", ""),
            new(AppSection.Reports, "Звіти", ""),
            new(AppSection.Users, "Користувачі", ""),
        }.ToDictionary(info => info.Section);

    public static SectionInfo Get(AppSection section) => Sections[section];
}
