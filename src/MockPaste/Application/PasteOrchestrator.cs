using MockPaste.Core.Generators;
using MockPaste.Core.Models;
using MockPaste.Infrastructure;
using MockPaste.Infrastructure.Native;

namespace MockPaste.Application;

public sealed class PasteOrchestrator
{
    private readonly GeneratorRegistry _generators;
    private readonly ClipboardService _clipboard;
    private readonly InputSimulationService _inputSimulation;
    private readonly AppSettings _settings;
    private readonly HistoryService _history;
    private readonly IAppLogger _logger;
    // Ensures only one paste pipeline runs at a time; prevents concurrent clipboard save/restore corruption.
    private readonly SemaphoreSlim _pasteLock = new(1, 1);

    public PasteOrchestrator(
        GeneratorRegistry generators,
        ClipboardService clipboard,
        InputSimulationService inputSimulation,
        AppSettings settings,
        HistoryService history,
        IAppLogger logger)
    {
        _generators = generators;
        _clipboard = clipboard;
        _inputSimulation = inputSimulation;
        _settings = settings;
        _history = history;
        _logger = logger;
    }

    public async Task ExecuteAsync(string categoryName, string formatId, IntPtr targetWindow, CancellationToken ct = default)
    {
        try
        {
            var generator = _generators.Get(categoryName);
            if (generator is null)
            {
                _logger.Warning($"Generator not found: {categoryName}");
                return;
            }

            var options = new FakeDataOptions { FormatId = formatId };
            var value = generator.Generate(options);
            _logger.Information($"Generated {categoryName}/{formatId}: {value.Length} chars");

            var formatName = generator.SupportedFormats.FirstOrDefault(f => f.FormatId == formatId)?.Name ?? formatId;

            bool pasted = await ExecuteCoreAsync(value, targetWindow, ct);

            if (pasted)
            {
                _history.Add(new HistoryEntry(value, categoryName, formatName, DateTime.UtcNow));
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Information($"Paste cancelled for {categoryName}/{formatId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Paste orchestration failed for {categoryName}/{formatId}", ex);
        }
    }

    public async Task ExecuteDirectAsync(string value, IntPtr targetWindow, CancellationToken ct = default)
    {
        try
        {
            _logger.Information($"Pasting from history: {value.Length} chars");

            bool pasted = await ExecuteCoreAsync(value, targetWindow, ct);

            if (pasted)
            {
                _history.Promote(value);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Information("History paste cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error("History paste failed", ex);
        }
    }

    /// <summary>
    /// Runs the shared clipboard-set → focus → paste → restore pipeline.
    /// Returns true if the paste simulation was dispatched successfully.
    /// All clipboard calls are marshalled to the UI dispatcher (STA thread).
    /// </summary>
    private async Task<bool> ExecuteCoreAsync(string value, IntPtr targetWindow, CancellationToken ct)
    {
        await _pasteLock.WaitAsync(ct);
        try
        {
        var dispatcher = System.Windows.Application.Current.Dispatcher;

        if (_settings.PreserveClipboard)
        {
            await dispatcher.InvokeAsync(() => _clipboard.TrySaveClipboard());
        }

        bool clipboardSet = await dispatcher.InvokeAsync(() => _clipboard.TrySetTextInstance(value));
        if (!clipboardSet)
        {
            _logger.Error("Failed to set clipboard text");
            return false;
        }

        if (!NativeMethods.SetForegroundWindow(targetWindow))
        {
            _logger.Warning("Failed to set foreground window — paste target may not receive focus");
        }

        await Task.Delay(_settings.PasteDelayMs, ct);

        bool pasted = _inputSimulation.SimulatePaste();
        if (!pasted)
        {
            _logger.Warning("Paste simulation may have failed");
        }

        if (_settings.PreserveClipboard)
        {
            await Task.Delay(_settings.ClipboardRestoreDelayMs, ct);

            bool restored = await dispatcher.InvokeAsync(() => _clipboard.TryRestoreClipboard());
            if (!restored)
            {
                _logger.Warning("Clipboard restore failed");
            }
        }

        return pasted;
        }
        finally
        {
            _pasteLock.Release();
        }
    }
}
