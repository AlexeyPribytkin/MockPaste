using System.ComponentModel;
using System.Windows.Input;
using MockPaste.Core.Models;
using MockPaste.UI.Settings;

namespace MockPaste.Tests.UI.Settings;

public sealed class SettingsViewModelTests
{
    private static AppSettings DefaultSettings() => new();

    private static SettingsViewModel CreateVm(AppSettings? settings = null)
    {
        var vm = new SettingsViewModel(settings ?? DefaultSettings());
        vm.ResourceResolver = key => key;
        return vm;
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

    [Fact]
    public void WhenPreserveClipboardChanges_IsDirty_IsTrue()
    {
        var vm = CreateVm();

        vm.PreserveClipboard = !vm.PreserveClipboard;

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void WhenPasteDelayTextChanges_IsDirty_IsTrue()
    {
        var vm = CreateVm();

        vm.PasteDelayText = "99";

        Assert.True(vm.IsDirty);
    }

    [Fact]
    public void WhenHistorySizeTextChanges_IsDirty_IsTrue()
    {
        var vm = CreateVm();

        vm.HistorySizeText = "50";

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

    [Fact]
    public void WhenSaveSucceeds_StatusMessage_IsNotEmpty()
    {
        var settings = DefaultSettings();
        var vm = CreateVm(settings);
        vm.SettingsSaved = _ => true;
        vm.PreserveClipboard = !vm.PreserveClipboard;

        vm.SaveCommand.Execute(null);

        Assert.NotEmpty(vm.StatusMessage);
    }

    [Fact]
    public void WhenSaveFails_StatusMessage_IsNotEmpty()
    {
        var settings = DefaultSettings();
        var vm = CreateVm(settings);
        vm.SettingsSaved = _ => false;
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
}
