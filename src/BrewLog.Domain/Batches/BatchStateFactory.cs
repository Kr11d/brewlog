using BrewLog.Domain.Common;

namespace BrewLog.Domain.Batches;

/// <summary>Фабрика станів: перетворює збережений у БД код на об'єкт стану. Єдине місце, де код стану зіставляється з класом.</summary>
public static class BatchStateFactory
{
    /// <summary>Усі етапи по порядку, наприклад для колонок дошки.</summary>
    public static IReadOnlyList<BatchState> All { get; } =
        [new PlannedState(), new BrewingState(), new FermentingState(), new ConditioningState(), new PackagedState()];

    public static BatchState FromCode(string code) => code switch
    {
        "Planned" => new PlannedState(),
        "Brewing" => new BrewingState(),
        "Fermenting" => new FermentingState(),
        "Conditioning" => new ConditioningState(),
        "Packaged" => new PackagedState(),
        _ => throw new DomainException($"Невідомий етап партії: {code}."),
    };
}
