using BrewLog.Domain.Abstractions;
using BrewLog.Domain.Batches;
using BrewLog.Domain.Common;
using BrewLog.Domain.Recipes;
using BrewLog.Domain.Users;

namespace BrewLog.Domain.Services;

/// <summary>Партії: планування, перехід між етапами зі списанням сировини, виміри, розлив і попередження.</summary>
public sealed class BatchService
{
    private readonly IBatchRepository _batches;
    private readonly IRecipeRepository _recipes;
    private readonly IStyleRepository _styles;
    private readonly RecipeService _recipeService;
    private readonly InventoryService _inventory;
    private readonly ICurrentSession _session;
    private readonly IClock _clock;

    public BatchService(
        IBatchRepository batches,
        IRecipeRepository recipes,
        IStyleRepository styles,
        RecipeService recipeService,
        InventoryService inventory,
        ICurrentSession session,
        IClock clock)
    {
        _batches = batches;
        _recipes = recipes;
        _styles = styles;
        _recipeService = recipeService;
        _inventory = inventory;
        _session = session;
        _clock = clock;
    }

    public IReadOnlyList<Batch> GetAll()
    {
        _session.Require(Permission.ViewBatches);
        return _batches.GetAll().Where(HasAccess).ToList();
    }

    public IReadOnlyList<Batch> GetPlanned()
    {
        _session.Require(Permission.ViewBatches);
        return _batches.GetPlanned().Where(HasAccess).ToList();
    }

    public IReadOnlyList<Batch> GetForAssistant(int userId)
    {
        _session.Require(Permission.ViewBatches);
        return _batches.GetForAssistant(userId).Where(HasAccess).ToList();
    }

    /// <summary>Партія за ідентифікатором; null, якщо її немає або користувач не має до неї доступу.</summary>
    public Batch? Get(int id)
    {
        _session.Require(Permission.ViewBatches);

        var batch = _batches.GetById(id);
        return batch is not null && HasAccess(batch) ? batch : null;
    }

    /// <summary>
    /// Планує партію й зберігає знімок рецепта: назву, стиль, планові показники й діапазон температур дріжджів.
    /// Подальші зміни рецепта вже запланованої партії не торкаються.
    /// </summary>
    public Batch Plan(int recipeId, double volumeL, DateTime plannedDate, int? assistantId)
    {
        _session.Require(Permission.ManageBatches);
        if (!(volumeL > 0) || !NumberLimits.IsAllowed(volumeL))
        {
            throw new DomainException("Обсяг партії має бути більшим за 0.");
        }

        var recipe = FindRecipe(recipeId);
        var stats = _recipeService.Calculate(ScaledTo(recipe, volumeL));
        var batch = new Batch
        {
            Number = BatchNumberGenerator.Next(_clock.Now.Year, _batches.GetNumbers()),
            RecipeId = recipe.Id,
            RecipeName = recipe.Name,
            StyleName = recipe.StyleId is { } styleId ? _styles.GetById(styleId)?.Name : null,
            VolumeL = volumeL,
            PlannedDate = plannedDate,
            PlannedOg = stats.Og,
            PlannedFg = stats.Fg,
            PlannedAbv = stats.Abv,
            PlannedSrm = stats.Srm,
            TempMinC = stats.TempMinC,
            TempMaxC = stats.TempMaxC,
            AssistantId = assistantId,
            CreatedBy = _session.User?.Id,
        };

        _batches.Insert(batch);
        return batch;
    }

    public TransitionCheck CanAdvance(int batchId)
    {
        _session.Require(Permission.ManageBatches);

        var batch = FindBatch(batchId);
        return batch.State.CanAdvance(ContextFor(batch, _clock.Now));
    }

    /// <summary>
    /// Переводить партію на наступний етап; вихід із запланованого списує сировину.
    /// Спершу списання, потім зміна етапу: якщо списання впало, партія лишається на місці без вартості.
    /// </summary>
    public void Advance(int batchId)
    {
        _session.Require(Permission.ManageBatches);

        var batch = FindBatch(batchId);
        var context = ContextFor(batch, _clock.Now);
        var check = batch.State.CanAdvance(context);
        if (!check.Allowed)
        {
            throw new DomainException(check.Reason ?? "Перехід на наступний етап заборонено.");
        }

        if (batch.State.RequiresStock)
        {
            batch.IngredientsCost = _inventory.WriteOffForBatch(batch.Id, FindRecipe(batch.RecipeId), batch.VolumeL, _session.User?.Id);
        }

        batch.Advance(context);
        _batches.UpdateState(batch);
    }

    public void SetActualOg(int batchId, double og)
    {
        _session.Require(Permission.ManageBatches);

        var batch = FindBatch(batchId);
        batch.SetActualOg(og);
        _batches.UpdateState(batch);
    }

    public Measurement AddMeasurement(int batchId, DateTime at, double? gravity, double? tempC, string? note)
    {
        _session.Require(Permission.AddMeasurements);

        var batch = FindAccessibleBatch(batchId);
        var measurement = batch.AddMeasurement(at, gravity, tempC, note, _session.User?.Id, _clock.Now);
        _batches.AddMeasurement(measurement);
        return measurement;
    }

    /// <summary>Оформлює розлив поточною датою й одразу завершує партію: обидві зміни зберігаються разом.</summary>
    public void Package(int batchId, Packaging p)
    {
        _session.Require(Permission.PackageBatches);

        var batch = FindAccessibleBatch(batchId);
        var now = _clock.Now;
        batch.SetPackaging(p with { PackagedAt = now, UserId = _session.User?.Id });
        batch.Advance(new TransitionContext(batch, now, null));
        _batches.SavePackaging(batch);
    }

    public IReadOnlyList<FermentationAlert> GetAlerts(int batchId)
    {
        _session.Require(Permission.ViewBatches);
        return FermentationAnalyzer.Analyze(FindAccessibleBatch(batchId), _clock.Now);
    }

    /// <summary>Копія рецепта на обсяг партії: кількості множаться на масштаб (HANDOFF §6.2).</summary>
    private static Recipe ScaledTo(Recipe recipe, double volumeL)
    {
        var scale = recipe.ScaleFactor(volumeL);
        var scaled = recipe.CopyAs(recipe.Name);
        scaled.BatchVolumeL = volumeL;
        foreach (var line in scaled.Ingredients)
        {
            line.Amount *= scale;
        }

        return scaled;
    }

    private TransitionContext ContextFor(Batch batch, DateTime now) => new(
        batch,
        now,
        batch.State.RequiresStock ? _inventory.CheckAvailability(FindRecipe(batch.RecipeId), batch.VolumeL) : null);

    private bool HasAccess(Batch batch) => _session.User?.HasAccessToBatchOf(batch.AssistantId) ?? true;

    private Batch FindBatch(int id) => _batches.GetById(id) ?? throw new DomainException("Партію не знайдено.");

    private Batch FindAccessibleBatch(int id)
    {
        var batch = FindBatch(id);
        return HasAccess(batch) ? batch : throw new DomainException("Ця партія призначена іншому помічнику.");
    }

    private Recipe FindRecipe(int id) => _recipes.GetById(id) ?? throw new DomainException("Рецепт не знайдено.");
}
