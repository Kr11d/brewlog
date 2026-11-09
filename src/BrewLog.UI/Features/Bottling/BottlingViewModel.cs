using BrewLog.Data.Export;
using BrewLog.Domain.Batches;
using BrewLog.Domain.Common;
using BrewLog.Domain.Services;
using BrewLog.UI.Features.Common;
using BrewLog.UI.Formatting;
using BrewLog.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BrewLog.UI.Features.Bottling;

/// <summary>Вікно розливу: живий підсумок за введеними пляшками й кегами, підтвердження й друк етикеток. Правила й розрахунки — у Batch.</summary>
public sealed partial class BottlingViewModel : ObservableObject
{
    private const double DefaultBottleVolume = 0.5;
    private const double DefaultKegVolume = 20;

    private readonly int _batchId;
    private readonly BatchService _batches;
    private readonly DialogService _dialogs;
    private readonly LabelPdfGenerator _labels;

    private Batch _batch;
    private double _bottles;
    private double _kegs;
    private double _bottleVolume = DefaultBottleVolume;
    private double _kegVolume = DefaultKegVolume;
    private double _finalGravity;

    [ObservableProperty]
    private string _packagedText = "—";

    [ObservableProperty]
    private string _lossText = "—";

    [ObservableProperty]
    private string _abvText = "—";

    [ObservableProperty]
    private string _validationText = string.Empty;

    [ObservableProperty]
    private bool _canConfirm;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    [NotifyPropertyChangedFor(nameof(CanPrint))]
    private bool _isPackaged;

    public BottlingViewModel(int batchId, BatchService batches, DialogService dialogs, LabelPdfGenerator labels)
    {
        _batchId = batchId;
        _batches = batches;
        _dialogs = dialogs;
        _labels = labels;
        _batch = LoadBatch();

        _finalGravity = _batch.CurrentGravity ?? _batch.PlannedFg;
        BottlesField = new NumberField(0, value => _bottles = value, Refresh);
        KegsField = new NumberField(0, value => _kegs = value, Refresh);
        FinalGravityField = new NumberField(_finalGravity, value => _finalGravity = value, Refresh, DisplayFormat.Gravity);
        Refresh();
    }

    public string Title => $"{_batch.Number} · {_batch.RecipeName}";

    public string VolumeText => DisplayFormat.Amount(_batch.VolumeL, "л");

    public NumberField BottlesField { get; }

    public NumberField KegsField { get; }

    public NumberField FinalGravityField { get; }

    public IReadOnlyList<VolumeOption> BottleVolumes { get; } =
    [
        new(0.33, "0,33 л", false),
        new(DefaultBottleVolume, "0,5 л", true),
        new(0.75, "0,75 л", false),
    ];

    public IReadOnlyList<VolumeOption> KegVolumes { get; } =
    [
        new(DefaultKegVolume, "20 л", true),
        new(30, "30 л", false),
        new(50, "50 л", false),
    ];

    public bool IsEditable => !IsPackaged;

    /// <summary>Етикетки доступні після розливу й лише для пляшок.</summary>
    public bool CanPrint => IsPackaged && _batch.Packaging is { BottlesCount: > 0 };

    [RelayCommand]
    private void SelectBottleVolume(double volume)
    {
        _bottleVolume = volume;
        Refresh();
    }

    [RelayCommand]
    private void SelectKegVolume(double volume)
    {
        _kegVolume = volume;
        Refresh();
    }

    [RelayCommand]
    private void Confirm()
    {
        if (!CanConfirm
            || !_dialogs.Confirm($"Оформити розлив партії {_batch.Number}: {PackagedText}, втрати {LossText}? Партія перейде на етап «Розлито».", "Підтвердити розлив", "Підтвердити"))
        {
            return;
        }

        try
        {
            _batches.Package(_batchId, BuildPackaging());
            _batch = LoadBatch();
            IsPackaged = true;
            CanConfirm = false;
        }
        catch (DomainException exception)
        {
            _dialogs.ShowError(exception.Message);
        }
    }

    /// <summary>Друкує етикетки у фоні: команда вимикає кнопку на час роботи.</summary>
    [RelayCommand]
    private async Task PrintLabelsAsync()
    {
        if (!CanPrint)
        {
            return;
        }

        var path = _dialogs.PickFileToSave("PDF (*.pdf)|*.pdf", "Друк етикеток", $"Етикетки-{_batch.Number}.pdf");
        if (path is not null)
        {
            var batch = _batch;
            await _dialogs.SaveAndOpenAsync(() => _labels.Save(batch, path), path);
        }
    }

    private Batch LoadBatch() => _batches.Get(_batchId) ?? throw new DomainException("Партію не знайдено.");

    private Packaging BuildPackaging() => new()
    {
        BottlesCount = (int)_bottles,
        BottleVolumeL = _bottleVolume,
        KegsCount = (int)_kegs,
        KegVolumeL = _kegVolume,
        FinalGravity = _finalGravity,
    };

    /// <summary>Перераховує підсумок на кожну зміну форми; неправильні поля вимикають підтвердження.</summary>
    private void Refresh()
    {
        if (!BottlesField.IsValid || !KegsField.IsValid || !FinalGravityField.IsValid
            || _bottles != Math.Floor(_bottles) || _kegs != Math.Floor(_kegs))
        {
            ValidationText = "Кількість пляшок і кег — цілі числа, кінцева густина — число.";
            PackagedText = LossText = AbvText = "—";
            CanConfirm = false;
            return;
        }

        var packaging = BuildPackaging();
        var loss = _batch.LossFor(packaging);
        var errors = _batch.ValidatePackaging(packaging);

        PackagedText = DisplayFormat.Amount(packaging.PackagedVolumeL, "л");
        LossText = $"{DisplayFormat.Amount(loss, "л")} ({DisplayFormat.Abv(loss / _batch.VolumeL * 100)})";
        AbvText = _batch.AbvAt(_finalGravity) is { } abv ? DisplayFormat.Abv(abv) : "—";
        ValidationText = string.Join(Environment.NewLine, errors);
        CanConfirm = errors.Count == 0;
    }
}
