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

    public async Task ExecuteAsync(string categoryName, string formatId, IntPtr targetWindow)
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
            _history.Add(new HistoryEntry(value, categoryName, formatName, DateTime.Now));

            if (_settings.PreserveClipboard)
                _clipboard.TrySaveClipboard();

            if (!_clipboard.TrySetText(value))
            {
                _logger.Error("Failed to set clipboard text");
                return;
            }

            NativeMethods.SetForegroundWindow(targetWindow);
            await Task.Delay(_settings.PasteDelayMs);

            if (!_inputSimulation.SimulatePaste())
                _logger.Warning("Paste simulation may have failed");

            if (_settings.PreserveClipboard)
            {
                await Task.Delay(100);
                _clipboard.TryRestoreClipboard();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Paste orchestration failed for {categoryName}/{formatId}", ex);
        }
    }

    public async Task ExecuteDirectAsync(string value, IntPtr targetWindow)
    {
        try
        {
            _logger.Information($"Pasting from history: {value.Length} chars");
            _history.Promote(value);

            if (_settings.PreserveClipboard)
                _clipboard.TrySaveClipboard();

            if (!_clipboard.TrySetText(value))
            {
                _logger.Error("Failed to set clipboard text");
                return;
            }

            NativeMethods.SetForegroundWindow(targetWindow);
            await Task.Delay(_settings.PasteDelayMs);

            if (!_inputSimulation.SimulatePaste())
                _logger.Warning("Paste simulation may have failed");

            if (_settings.PreserveClipboard)
            {
                await Task.Delay(100);
                _clipboard.TryRestoreClipboard();
            }
        }
        catch (Exception ex)
        {
            _logger.Error("History paste failed", ex);
        }
    }
}
