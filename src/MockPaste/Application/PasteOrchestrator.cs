using MockPaste.Core.Generators;
using MockPaste.Core.Models;
using MockPaste.Infrastructure;
using MockPaste.Infrastructure.Native;

namespace MockPaste.Application;

/// <summary>
/// Coordinates the full paste pipeline: generate a fake value → save the clipboard
/// → set new clipboard text → focus the target window → simulate Ctrl+V → restore
/// the original clipboard. Marshals all clipboard operations to the UI (STA) thread.
/// </summary>
public sealed class PasteOrchestrator
{
    private readonly GeneratorRegistry _generators;
    private readonly ClipboardService _clipboard;
    private readonly InputSimulationService _inputSimulation;
    private readonly AppSettings _settings;
    private readonly HistoryService _history;
    private readonly IAppLogger _logger;

    /// <summary>Creates the orchestrator and injects all required service dependencies.</summary>
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

    /// <summary>
    /// Generates a value using the named <paramref name="categoryName"/> generator and
    /// <paramref name="formatId"/> format, then runs the clipboard-paste pipeline.
    /// The generated value is added to history on success.
    /// </summary>
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

    /// <summary>
    /// Pastes <paramref name="value"/> directly (e.g. from the history list) without
    /// invoking a generator. Promotes the entry to the top of the history list on success.
    /// </summary>
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

        if (!ForceForegroundWindow(targetWindow))
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
    /// <summary>
    /// Sets <paramref name="hWnd"/> as the foreground window, bypassing the Windows
    /// foreground-lock by temporarily attaching our thread's input queue to the
    /// target thread's input queue. This is necessary when our foreground right
    /// has already been consumed (e.g. by <c>Activate()</c> on the popup) before
    /// the paste pipeline runs.
    /// </summary>
    private static bool ForceForegroundWindow(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            return false;
        }

        uint targetThread = NativeMethods.GetWindowThreadProcessId(hWnd, out _);
        uint currentThread = NativeMethods.GetCurrentThreadId();

        bool attached = targetThread != currentThread
            && NativeMethods.AttachThreadInput(currentThread, targetThread, true);

        bool result = NativeMethods.SetForegroundWindow(hWnd);

        if (attached)
        {
            NativeMethods.AttachThreadInput(currentThread, targetThread, false);
        }

        return result;
    }
}
