using BrewLog.Domain.Common;

namespace BrewLog.Domain.Batches;

/// <summary>
/// Етап партії (патерн State, ієрархія ЛР 1). Кожен стан сам вирішує, чи можна йти далі й що дозволено на ньому,
/// тому в коді немає перевірок на кшталт «якщо партія в бродінні».
/// </summary>
public abstract class BatchState
{
    /// <summary>Значення колонки batches.state.</summary>
    public abstract string Code { get; }

    public abstract string DisplayName { get; }

    /// <summary>Номер етапу, починаючи з 1: для шкали й сортування.</summary>
    public abstract int Order { get; }

    public virtual bool AllowsMeasurements => false;

    public virtual bool AllowsActualOg => false;

    public virtual bool AllowsPackaging => false;

    /// <summary>Чи триває бродіння: лише тоді мають сенс попередження про його хід.</summary>
    public virtual bool TracksFermentation => false;

    /// <summary>Чи потребує вихід з етапу сировини: тоді сервіс перевіряє склад і списує її під час переходу.</summary>
    public virtual bool RequiresStock => false;

    /// <summary>Перевіряє умову виходу з цього етапу (HANDOFF §6.3); причина відмови призначена для користувача.</summary>
    public abstract TransitionCheck CanAdvance(TransitionContext ctx);

    /// <summary>Наступний етап; фінальний етап повертає сам себе.</summary>
    public abstract BatchState Next();

    /// <summary>Дія під час входу на етап: ставить його мітку часу.</summary>
    public virtual void OnEnter(Batch batch, DateTime now)
    {
    }

    /// <summary>Коли партія потрапила на цей етап; null, якщо мітки немає.</summary>
    public abstract DateTime? EnteredAt(Batch batch);
}
