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

    public PasteOrchestrator(
        GeneratorRegistry generators,
        ClipboardService clipboard,
        InputSimulationService inputSimulation,
        AppSettings settings,
        HistoryService history)
    {
        _generators = generators;
        _clipboard = clipboard;
        _inputSimulation = inputSimulation;
        _settings = settings;
        _history = history;
    }

    public async Task ExecuteAsync(string categoryName, string formatId, IntPtr targetWindow)
    {
        try
        {
            var generator = _generators.Get(categoryName);
            if (generator is null)
            {
                AppLogger.Warning($"Generator not found: {categoryName}");
                return;
            }

            var options = new FakeDataOptions { FormatId = formatId };
            var value = generator.Generate(options);
            AppLogger.Information($"Generated {categoryName}/{formatId}: {value.Length} chars");

            var formatName = generator.SupportedFormats.FirstOrDefault(f => f.FormatId == formatId)?.Name ?? formatId;
            _history.Add(new HistoryEntry(value, categoryName, formatName, DateTime.Now));

            if (_settings.PreserveClipboard)
                _clipboard.TrySaveClipboard();

            if (!_clipboard.TrySetText(value))
            {
                AppLogger.Error("Failed to set clipboard text");
                return;
            }

            NativeMethods.SetForegroundWindow(targetWindow);
            await Task.Delay(_settings.PasteDelayMs);

            if (!_inputSimulation.SimulatePaste())
                AppLogger.Warning("Paste simulation may have failed");

            if (_settings.PreserveClipboard)
            {
                await Task.Delay(100);
                _clipboard.TryRestoreClipboard();
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Paste orchestration failed for {categoryName}/{formatId}", ex);
        }
    }

    public async Task ExecuteDirectAsync(string value, IntPtr targetWindow)
    {
        try
        {
            AppLogger.Information($"Pasting from history: {value.Length} chars");
            _history.Promote(value);

            if (_settings.PreserveClipboard)
                _clipboard.TrySaveClipboard();

            if (!_clipboard.TrySetText(value))
            {
                AppLogger.Error("Failed to set clipboard text");
                return;
            }

            NativeMethods.SetForegroundWindow(targetWindow);
            await Task.Delay(_settings.PasteDelayMs);

            if (!_inputSimulation.SimulatePaste())
                AppLogger.Warning("Paste simulation may have failed");

            if (_settings.PreserveClipboard)
            {
                await Task.Delay(100);
                _clipboard.TryRestoreClipboard();
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("History paste failed", ex);
        }
    }
}
