using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Batches;

namespace BrewLog.Tests.Fakes;

/// <summary>Заглушка сховища партій: тримає об'єкти в пам'яті й рахує збереження етапу.</summary>
public sealed class FakeBatchRepository : InMemoryRepository<Batch>, IBatchRepository
{
    private int _nextMeasurementId = 1;

    public int StateUpdates { get; private set; }

    public IReadOnlyList<Batch> GetPlanned() => GetAll().Where(batch => batch.State.RequiresStock).ToList();

    public IReadOnlyList<Batch> GetForAssistant(int userId) =>
        GetAll().Where(batch => batch.AssistantId is null || batch.AssistantId == userId).ToList();

    public IReadOnlyList<string> GetNumbers() => GetAll().Select(batch => batch.Number).ToList();

    public int AddMeasurement(Measurement measurement) => measurement.Id = _nextMeasurementId++;

    public void SavePackaging(Batch batch) => StateUpdates++;

    public void UpdateState(Batch batch) => StateUpdates++;
}
