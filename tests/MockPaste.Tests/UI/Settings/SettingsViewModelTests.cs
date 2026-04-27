using System.ComponentModel;
using System.Windows.Input;
using MockPaste.Core.Models;
using MockPaste.Resources;
using MockPaste.UI.Settings;

namespace MockPaste.Tests.UI.Settings;

public sealed class SettingsViewModelTests
{
    private static AppSettings DefaultSettings() => new();

    private static SettingsViewModel CreateVm(AppSettings? settings = null)
    {
        return new SettingsViewModel(settings ?? DefaultSettings());
    }

    // ── Initial state ─────────────────────────────────────────────────────

    [Fact]
    public void InitialHotkeyDisplayText_MatchesSettingsHotkey()
    {
        var settings = DefaultSettings();
        var vm = CreateVm(settings);

        Assert.Equal(settings.Hotkey.ToDisplayString(), vm.HotkeyDisplayText);
    }

    [Fact]
    public void InitialIsDirty_IsFalse()
    {
        var vm = CreateVm();

        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void InitialIsCapturing_IsFalse()
    {
        var vm = CreateVm();

        Assert.False(vm.IsCapturing);
    }

    // ── IsDirty ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("PreserveClipboard")]
    [InlineData("PasteDelay")]
    [InlineData("HistorySize")]
    public void WhenEditableSettingChanges_IsDirty_IsTrue(string settingName)
    {
        var vm = CreateVm();

        switch (settingName)
        {
            case "PreserveClipboard":
                vm.PreserveClipboard = !vm.PreserveClipboard;
                break;
            case "PasteDelay":
                vm.PasteDelayText = "99";
                break;
            case "HistorySize":
                vm.HistorySizeText = "50";
                break;
            default:
                throw new ArgumentException("Unknown setting name.", nameof(settingName));
        }

        Assert.True(vm.IsDirty);
    }

    // ── Validation ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("0", true)]
    [InlineData("50", true)]
    [InlineData("500", true)]
    [InlineData("-1", false)]
    [InlineData("501", false)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    public void IsPasteDelayValid_ReflectsAllowedRange(string text, bool expected)
    {
        var vm = CreateVm();

        vm.PasteDelayText = text;

        Assert.Equal(expected, vm.IsPasteDelayValid);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("10", true)]
    [InlineData("500", true)]
    [InlineData("0", false)]
    [InlineData("501", false)]
    [InlineData("abc", false)]
    public void IsHistorySizeValid_ReflectsAllowedRange(string text, bool expected)
    {
        var vm = CreateVm();

        vm.HistorySizeText = text;

        Assert.Equal(expected, vm.IsHistorySizeValid);
    }

    // ── Capture flow ──────────────────────────────────────────────────────

    [Fact]
    public void WhenChangeHotkeyCommandExecuted_IsCapturing_IsTrue()
    {
        var vm = CreateVm();

        vm.ChangeHotkeyCommand.Execute(null);

        Assert.True(vm.IsCapturing);
    }

    [Fact]
    public void WhenCancelCaptureCalledDuringCapture_IsCapturing_IsFalse()
    {
        var vm = CreateVm();
        vm.ChangeHotkeyCommand.Execute(null);

        vm.CancelCapture();

        Assert.False(vm.IsCapturing);
    }

    [Fact]
    public void WhenCancelCaptureCalledDuringCapture_HotkeyDisplayText_RestoredToPending()
    {
        var settings = DefaultSettings();
        var vm = CreateVm(settings);
        vm.ChangeHotkeyCommand.Execute(null);

        vm.CancelCapture();

        Assert.Equal(settings.Hotkey.ToDisplayString(), vm.HotkeyDisplayText);
    }

    [Fact]
    public void WhenAcceptHotkeyCalled_IsCapturing_IsFalse()
    {
        var vm = CreateVm();
        vm.ChangeHotkeyCommand.Execute(null);
        var config = new HotkeyConfig { Ctrl = true, Key = Key.V };

        vm.AcceptHotkey(config);

        Assert.False(vm.IsCapturing);
    }

    [Fact]
    public void WhenAcceptHotkeyCalled_HotkeyDisplayText_UpdatedToNewHotkey()
    {
        var vm = CreateVm();
        vm.ChangeHotkeyCommand.Execute(null);
        var config = new HotkeyConfig { Ctrl = true, Key = Key.V };

        vm.AcceptHotkey(config);

        Assert.Equal(config.ToDisplayString(), vm.HotkeyDisplayText);
    }

    [Fact]
    public void WhenAcceptHotkeyCalled_IsDirty_IsTrue()
    {
        var vm = CreateVm();
        vm.ChangeHotkeyCommand.Execute(null);
        var config = new HotkeyConfig { Ctrl = true, Key = Key.V };

        vm.AcceptHotkey(config);

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void WhenChangeHotkeyCommandExecutedTwice_IsCapturing_IsFalse()
    {
        var vm = CreateVm();
        vm.ChangeHotkeyCommand.Execute(null);

        // Executing again while capturing cancels capture
        vm.ChangeHotkeyCommand.Execute(null);

        Assert.False(vm.IsCapturing);
    }

    // ── Reset hotkey ──────────────────────────────────────────────────────

    [Fact]
    public void WhenResetHotkeyCommandExecuted_HotkeyDisplayText_IsDefault()
    {
        var vm = CreateVm();
        vm.ChangeHotkeyCommand.Execute(null);
        vm.AcceptHotkey(new HotkeyConfig { Ctrl = true, Key = Key.V });

        vm.ResetHotkeyCommand.Execute(null);

        Assert.Equal(HotkeyConfig.Default.ToDisplayString(), vm.HotkeyDisplayText);
    }

    [Fact]
    public void WhenResetHotkeyCommandExecutedDuringCapture_IsCapturing_IsFalse()
    {
        var vm = CreateVm();
        vm.ChangeHotkeyCommand.Execute(null);

        vm.ResetHotkeyCommand.Execute(null);

        Assert.False(vm.IsCapturing);
    }

    // ── Save ──────────────────────────────────────────────────────────────

    [Fact]
    public void WhenSaveCommandExecuted_IsDirty_IsFalse()
    {
        var settings = DefaultSettings();
        var vm = CreateVm(settings);
        vm.SettingsSaved = _ => true;
        vm.PreserveClipboard = !vm.PreserveClipboard;

        vm.SaveCommand.Execute(null);

        Assert.False(vm.IsDirty);
    }

    [Fact]
    public void WhenSaveCommandExecuted_SettingsSavedCallback_IsInvoked()
    {
        var settings = DefaultSettings();
        var vm = CreateVm(settings);
        bool called = false;
        vm.SettingsSaved = _ => { called = true; return true; };
        vm.PreserveClipboard = !vm.PreserveClipboard;

        vm.SaveCommand.Execute(null);

        Assert.True(called);
    }

    [Fact]
    public void WhenPasteDelayInvalid_SaveCommand_CannotExecute()
    {
        var vm = CreateVm();
        vm.SettingsSaved = _ => true;
        vm.PasteDelayText = "abc";

        Assert.False(vm.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void WhenHistorySizeInvalid_SaveCommand_CannotExecute()
    {
        var vm = CreateVm();
        vm.SettingsSaved = _ => true;
        vm.HistorySizeText = "0";

        Assert.False(vm.SaveCommand.CanExecute(null));
    }

    // ── Status ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WhenSaveCommandExecuted_StatusMessage_IsNotEmpty(bool saveResult)
    {
        var settings = DefaultSettings();
        var vm = CreateVm(settings);
        vm.SettingsSaved = _ => saveResult;
        vm.PreserveClipboard = !vm.PreserveClipboard;

        vm.SaveCommand.Execute(null);

        Assert.NotEmpty(vm.StatusMessage);
    }

    [Fact]
    public void WhenAcceptHotkeyCalled_StatusMessage_IsNotEmpty()
    {
        var vm = CreateVm();
        vm.ChangeHotkeyCommand.Execute(null);
        var config = new HotkeyConfig { Ctrl = true, Key = Key.V };

        vm.AcceptHotkey(config);

        Assert.NotEmpty(vm.StatusMessage);
    }

    [Fact]
    public void WhenStatusMessageChanges_PropertyChanged_IsRaisedForStatusMessage()
    {
        var vm = CreateVm();
        vm.SettingsSaved = _ => true;
        vm.PreserveClipboard = !vm.PreserveClipboard;
        string? changedProperty = null;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.StatusMessage))
            {
                changedProperty = e.PropertyName;
            }
        };

        vm.SaveCommand.Execute(null);

        Assert.Equal(nameof(SettingsViewModel.StatusMessage), changedProperty);
    }

    // ── FormatModifiers ───────────────────────────────────────────────────

    [Theory]
    [InlineData(ModifierKeys.None, "")]
    [InlineData(ModifierKeys.Control, "Ctrl + ")]
    [InlineData(ModifierKeys.Alt, "Alt + ")]
    [InlineData(ModifierKeys.Shift, "Shift + ")]
    [InlineData(ModifierKeys.Windows, "Win + ")]
    [InlineData(ModifierKeys.Control | ModifierKeys.Alt, "Ctrl + Alt + ")]
    [InlineData(ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift, "Ctrl + Alt + Shift + ")]
    public void FormatModifiers_ProducesExpectedString(ModifierKeys modifiers, string expected)
    {
        var result = SettingsViewModel.FormatModifiers(modifiers);

        Assert.Equal(expected, result);
    }

    // ── PasteDelay int property ───────────────────────────────────────────

    [Fact]
    public void PasteDelay_InitialValue_MatchesSettings()
    {
        var settings = new AppSettings { PasteDelayMs = 120 };
        var vm = CreateVm(settings);

        Assert.Equal(120, vm.PasteDelay);
    }

    [Fact]
    public void WhenPasteDelaySet_PasteDelayText_IsUpdated()
    {
        var vm = CreateVm();

        vm.PasteDelay = 75;

        Assert.Equal("75", vm.PasteDelayText);
    }

    [Fact]
    public void WhenPasteDelaySet_IsDirty_IsTrue()
    {
        var vm = CreateVm();

        vm.PasteDelay = 200;

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void WhenPasteDelaySet_PropertyChanged_IsRaisedForPasteDelay()
    {
        var vm = CreateVm();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.PasteDelay = 100;

        Assert.Contains(nameof(SettingsViewModel.PasteDelay), raised);
    }

    // ── HistorySize int property ──────────────────────────────────────────

    [Fact]
    public void HistorySize_InitialValue_MatchesSettings()
    {
        var settings = new AppSettings { HistorySize = 42 };
        var vm = CreateVm(settings);

        Assert.Equal(42, vm.HistorySize);
    }

    [Fact]
    public void WhenHistorySizeSet_HistorySizeText_IsUpdated()
    {
        var vm = CreateVm();

        vm.HistorySize = 25;

        Assert.Equal("25", vm.HistorySizeText);
    }

    [Fact]
    public void WhenHistorySizeSet_IsDirty_IsTrue()
    {
        var vm = CreateVm();

        vm.HistorySize = 30;

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void WhenHistorySizeSet_PropertyChanged_IsRaisedForHistorySize()
    {
        var vm = CreateVm();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.HistorySize = 15;

        Assert.Contains(nameof(SettingsViewModel.HistorySize), raised);
    }

    // ── PasteDelayDisplay ─────────────────────────────────────────────────

    [Fact]
    public void PasteDelayDisplay_ContainsValueAndUnit()
    {
        var vm = CreateVm();
        vm.PasteDelay = 80;

        Assert.Contains("80", vm.PasteDelayDisplay);
        Assert.Contains(Strings.StringUnitMilliseconds, vm.PasteDelayDisplay);
    }

    [Fact]
    public void WhenPasteDelayChanges_PropertyChanged_IsRaisedForPasteDelayDisplay()
    {
        var vm = CreateVm();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.PasteDelay = AppSettings.PasteDelayDefault + 10;

        Assert.Contains(nameof(SettingsViewModel.PasteDelayDisplay), raised);
    }

    // ── HistorySizeDisplay ────────────────────────────────────────────────

    [Fact]
    public void HistorySizeDisplay_WhenSizeIsOne_UsesSingularUnit()
    {
        var vm = CreateVm();
        vm.HistorySize = 1;

        Assert.Contains(Strings.StringUnitItem, vm.HistorySizeDisplay);
        Assert.DoesNotContain(Strings.StringUnitItems, vm.HistorySizeDisplay);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    [InlineData(500)]
    public void HistorySizeDisplay_WhenSizeIsNotOne_UsesPluralUnit(int size)
    {
        var vm = CreateVm();
        vm.HistorySize = size;

        Assert.Contains(Strings.StringUnitItems, vm.HistorySizeDisplay);
    }

    [Fact]
    public void HistorySizeDisplay_ContainsValue()
    {
        var vm = CreateVm();
        vm.HistorySize = 7;

        Assert.Contains("7", vm.HistorySizeDisplay);
    }

    [Fact]
    public void WhenHistorySizeChanges_PropertyChanged_IsRaisedForHistorySizeDisplay()
    {
        var vm = CreateVm();
        var raised = new List<string?>();
        vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        vm.HistorySize = 5;

        Assert.Contains(nameof(SettingsViewModel.HistorySizeDisplay), raised);
    }

    [Fact]
    public void WhenThemeLightSelected_OtherThemeFlags_AreFalse()
    {
        var vm = CreateVm();

        vm.IsThemeLight = true;

        Assert.True(vm.IsThemeLight);
        Assert.False(vm.IsThemeDark);
        Assert.False(vm.IsThemeSystem);
    }
}
