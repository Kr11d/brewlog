using BrewLog.Domain.Batches;

namespace BrewLog.Domain.Abstractions;

/// <summary>Сховище партій разом із вимірами й розливом.</summary>
public interface IBatchRepository : IRepository<Batch>
{
    IReadOnlyList<Batch> GetPlanned();

    /// <summary>Партії цього помічника та партії без виконавця.</summary>
    IReadOnlyList<Batch> GetForAssistant(int userId);

    /// <summary>Усі наявні номери партій: для генерації наступного.</summary>
    IReadOnlyList<string> GetNumbers();

    /// <summary>Додає вимір і записує його ідентифікатор; повертає цей ідентифікатор.</summary>
    int AddMeasurement(Measurement measurement);

    /// <summary>В одній транзакції зберігає розлив партії та її новий етап.</summary>
    void SavePackaging(Batch batch);

    /// <summary>Зберігає етап, дати етапів, фактичну OG і вартість сировини.</summary>
    void UpdateState(Batch batch);
}
