using BrewLog.Domain.Common;

namespace BrewLog.Domain.Batches;

/// <summary>
/// Партія пива. Етап, фактична OG, виміри й розлив змінюються лише через методи класу, які застосовують правила поточного етапу;
/// init-властивості потрібні, щоб відновити партію зі сховища.
/// </summary>
public sealed class Batch : IEntity
{
    private const double AbvFactor = 131.25;
    private const double VolumeTolerance = 1e-9;

    private BatchState _state = new PlannedState();
    private double? _actualOg;
    private List<Measurement> _measurements = [];
    private Packaging? _packaging;

    public int Id { get; set; }

    public string Number { get; init; } = string.Empty;

    public int RecipeId { get; init; }

    public string RecipeName { get; init; } = string.Empty;

    public string? StyleName { get; init; }

    public double VolumeL { get; init; }

    public BatchState State
    {
        get => _state;
        init => _state = value;
    }

    public DateTime PlannedDate { get; init; }

    public DateTime? BrewedAt { get; set; }

    public DateTime? FermentingAt { get; set; }

    public DateTime? ConditioningAt { get; set; }

    public DateTime? PackagedAt { get; set; }

    public double PlannedOg { get; init; }

    public double PlannedFg { get; init; }

    public double PlannedAbv { get; init; }

    public double PlannedSrm { get; init; }

    public double? TempMinC { get; init; }

    public double? TempMaxC { get; init; }

    public double? ActualOg
    {
        get => _actualOg;
        init => _actualOg = value;
    }

    /// <summary>Вартість списаної сировини, грн.</summary>
    public decimal? IngredientsCost { get; set; }

    public int? AssistantId { get; set; }

    public int? CreatedBy { get; init; }

    public string? Notes { get; set; }

    /// <summary>Виміри за часом, від найдавнішого.</summary>
    public IReadOnlyList<Measurement> Measurements
    {
        get => _measurements;
        init => _measurements = value.OrderBy(item => item.MeasuredAt).ToList();
    }

    public Packaging? Packaging
    {
        get => _packaging;
        init => _packaging = value;
    }

    /// <summary>Остання виміряна густина; виміри лише з температурою пропускаються.</summary>
    public double? CurrentGravity => _measurements.LastOrDefault(item => item.Gravity.HasValue)?.Gravity;

    /// <summary>Видиме зброджування, %: (OG − поточна густина) / (OG − 1).</summary>
    public double? ApparentAttenuation =>
        ActualOg is { } og && og > 1 && CurrentGravity is { } current ? (og - current) / (og - 1) * 100 : null;

    /// <summary>Фактична міцність, %, за HANDOFF §6.2; відома лише після розливу з кінцевою густиною.</summary>
    public double? ActualAbv =>
        ActualOg is { } og && Packaging?.FinalGravity is { } finalGravity ? (og - finalGravity) * AbvFactor : null;

    /// <summary>Чи лежить густина в межах, можливих для виміряного сусла чи пива.</summary>
    public static bool IsMeasurableGravity(double gravity) => gravity is >= 0.990 and <= 1.150;

    /// <summary>Скільки календарних днів партія на поточному етапі; для партії, запланованої наперед, 0.</summary>
    public int DaysInCurrentState(DateTime now) =>
        State.EnteredAt(this) is { } enteredAt ? Math.Max((now.Date - enteredAt.Date).Days, 0) : 0;

    /// <summary>Переводить партію на наступний етап, якщо поточний це дозволяє; за відмови нічого не змінює.</summary>
    public TransitionCheck Advance(TransitionContext ctx)
    {
        var check = State.CanAdvance(ctx);
        if (check.Allowed)
        {
            _state = State.Next();
            State.OnEnter(this, ctx.Now);
        }

        return check;
    }

    public void SetActualOg(double og)
    {
        if (!State.AllowsActualOg)
        {
            throw new DomainException($"На етапі «{State.DisplayName}» фактичну OG не вводять.");
        }

        if (og is not (>= 1.000 and <= 1.150))
        {
            throw new DomainException("Фактична OG має бути в межах 1,000–1,150.");
        }

        _actualOg = og;
    }

    /// <summary>Додає вимір на його місце за часом: вимір можна внести заднім числом.</summary>
    public Measurement AddMeasurement(DateTime at, double? gravity, double? tempC, string? note, int? userId, DateTime now)
    {
        if (!State.AllowsMeasurements)
        {
            throw new DomainException($"На етапі «{State.DisplayName}» виміри не вносять.");
        }

        var errors = new List<string>();
        if (gravity is null && tempC is null)
        {
            errors.Add("Вкажіть густину або температуру.");
        }

        if (gravity is { } value && !IsMeasurableGravity(value))
        {
            errors.Add("Густина має бути в межах 0,990–1,150.");
        }

        if (tempC is not (null or (>= -5 and <= 40)))
        {
            errors.Add("Температура має бути в межах −5…40 °C.");
        }

        if (at > now)
        {
            errors.Add("Дата виміру не може бути пізнішою за поточну.");
        }

        DomainException.ThrowIfAny(errors);

        var measurement = new Measurement
        {
            BatchId = Id,
            MeasuredAt = at,
            Gravity = gravity,
            TemperatureC = tempC,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            UserId = userId,
        };
        _measurements.Insert(_measurements.FindLastIndex(item => item.MeasuredAt <= at) + 1, measurement);
        return measurement;
    }

    /// <summary>Приймає розлив і рахує втрати: обсяг партії мінус розлите, не менше 0 (HANDOFF §6.2).</summary>
    public void SetPackaging(Packaging packaging)
    {
        if (!State.AllowsPackaging)
        {
            throw new DomainException($"На етапі «{State.DisplayName}» розлив не оформлюють.");
        }

        DomainException.ThrowIfAny(packaging.Validate());
        if (packaging.PackagedVolumeL > VolumeL + VolumeTolerance)
        {
            throw new DomainException("Розлитий обсяг не може перевищувати обсяг партії.");
        }

        _packaging = packaging with { BatchId = Id, LossL = Math.Max(VolumeL - packaging.PackagedVolumeL, 0) };
    }
}
