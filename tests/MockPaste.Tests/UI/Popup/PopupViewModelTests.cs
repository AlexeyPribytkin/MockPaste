using System.Collections.Generic;
using MockPaste.Core.Generators;
using MockPaste.Core.Models;
using MockPaste.Infrastructure;
using MockPaste.UI.Popup;

namespace MockPaste.Tests.UI.Popup;

public sealed class PopupViewModelTests
{
    // ── Test doubles ──────────────────────────────────────────────────────

    private sealed class StubGenerator : IFakeDataGenerator
    {
        public string CategoryName { get; }
        public string MnemonicKey { get; }
        public int Order { get; }
        public IReadOnlyList<DataFormat> SupportedFormats { get; }

        public StubGenerator(string category, string mnemonic, int order, params DataFormat[] formats)
        {
            CategoryName = category;
            MnemonicKey = mnemonic;
            Order = order;
            SupportedFormats = formats;
        }

        public string Generate(FakeDataOptions options) => string.Empty;
    }

    private static GeneratorRegistry BuildRegistry(params IFakeDataGenerator[] generators)
    {
        var registry = new GeneratorRegistry();
        foreach (var g in generators)
        {
            registry.Register(g);
        }
        return registry;
    }

    private static DataFormat Format(string id, string name) =>
        new() { FormatId = id, Name = name };

    private static PopupViewModel CreateVm(GeneratorRegistry? registry = null, HistoryService? history = null)
    {
        registry ??= BuildRegistry(
            new StubGenerator("Email", "E", 1, Format("email-basic", "Basic")),
            new StubGenerator("GUID", "G", 2, Format("guid-default", "Default"), Format("guid-upper", "Uppercase")));
        return new PopupViewModel(registry, history ?? new HistoryService());
    }

    // ── Initial / ShowCategories ──────────────────────────────────────────

    [Fact]
    public void ShowCategories_SetsHeaderToMockPaste()
    {
        var vm = CreateVm();

        vm.ShowCategories();

        Assert.Equal("StringAppName", vm.HeaderText);
    }

    [Fact]
    public void ShowCategories_IsBackButton_IsFalse()
    {
        var vm = CreateVm();
        vm.ShowFormats(new StubGenerator("Email", "E", 1, Format("id", "name")));

        vm.ShowCategories();

        Assert.False(vm.IsBackButton);
    }

    [Fact]
    public void ShowCategories_IsHistoryButtonVisible_IsTrue()
    {
        var vm = CreateVm();
        vm.ShowHistory();

        vm.ShowCategories();

        Assert.True(vm.IsHistoryButtonVisible);
    }

    [Fact]
    public void ShowCategories_Items_ContainsOneEntryPerGenerator()
    {
        var vm = CreateVm();

        vm.ShowCategories();

        Assert.Equal(2, vm.Items.Count);
    }

    [Fact]
    public void ShowCategories_Items_OrderedByGeneratorOrder()
    {
        var vm = CreateVm();

        vm.ShowCategories();

        var first = Assert.IsType<MenuItemViewModel>(vm.Items[0]);
        Assert.Equal("Email", first.CategoryName);
    }

    [Fact]
    public void ShowCategories_SelectedIndex_IsZero()
    {
        var vm = CreateVm();

        vm.ShowCategories();

        Assert.Equal(0, vm.SelectedIndex);
    }

    [Fact]
    public void ShowCategories_IsFormatLevel_IsFalse()
    {
        var vm = CreateVm();

        vm.ShowCategories();

        Assert.False(vm.IsFormatLevel);
    }

    [Fact]
    public void ShowCategories_IsHistoryLevel_IsFalse()
    {
        var vm = CreateVm();

        vm.ShowCategories();

        Assert.False(vm.IsHistoryLevel);
    }

    // ── ShowFormats ───────────────────────────────────────────────────────

    [Fact]
    public void ShowFormats_SetsHeaderWithBackArrowAndCategory()
    {
        var vm = CreateVm();
        var gen = new StubGenerator("Email", "E", 1, Format("email-basic", "Basic"));

        vm.ShowFormats(gen);

        Assert.Equal("StringPopupBackFormat", vm.HeaderText);
    }

    [Fact]
    public void ShowFormats_IsFormatLevel_IsTrue()
    {
        var vm = CreateVm();

        vm.ShowFormats(new StubGenerator("Email", "E", 1, Format("id", "name")));

        Assert.True(vm.IsFormatLevel);
    }

    [Fact]
    public void ShowFormats_IsHistoryButtonVisible_IsFalse()
    {
        var vm = CreateVm();

        vm.ShowFormats(new StubGenerator("Email", "E", 1, Format("id", "name")));

        Assert.False(vm.IsHistoryButtonVisible);
    }

    [Fact]
    public void ShowFormats_Items_ContainsOneEntryPerFormat()
    {
        var vm = CreateVm();
        var gen = new StubGenerator("GUID", "G", 1, Format("guid-default", "Default"), Format("guid-upper", "Uppercase"));

        vm.ShowFormats(gen);

        Assert.Equal(2, vm.Items.Count);
    }

    [Fact]
    public void ShowFormats_SelectedIndex_IsZero()
    {
        var vm = CreateVm();

        vm.ShowFormats(new StubGenerator("Email", "E", 1, Format("id", "name")));

        Assert.Equal(0, vm.SelectedIndex);
    }

    // ── ShowHistory ───────────────────────────────────────────────────────

    [Fact]
    public void ShowHistory_SetsHistoryHeader()
    {
        var vm = CreateVm();

        vm.ShowHistory();

        Assert.Equal("StringPopupBackFormat", vm.HeaderText);
    }

    [Fact]
    public void ShowHistory_IsHistoryLevel_IsTrue()
    {
        var vm = CreateVm();

        vm.ShowHistory();

        Assert.True(vm.IsHistoryLevel);
    }

    [Fact]
    public void ShowHistory_WhenEmpty_ShowsEmptyHistoryState()
    {
        var vm = CreateVm(history: new HistoryService());

        vm.ShowHistory();

        Assert.True(vm.IsEmptyHistoryVisible);
        Assert.Empty(vm.Items);
    }

    [Fact]
    public void ShowHistory_WhenHasEntries_Items_ContainsHistoryViewModels()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("value1", "Email", "Basic", DateTime.Now));
        history.Add(new HistoryEntry("value2", "GUID", "Default", DateTime.Now));
        var vm = CreateVm(history: history);

        vm.ShowHistory();

        Assert.Equal(2, vm.Items.Count);
        Assert.All(vm.Items, item => Assert.IsType<HistoryItemViewModel>(item));
    }

    [Fact]
    public void ShowHistory_WhenHasEntries_IsEmptyHistoryVisible_IsFalse()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("value1", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);

        vm.ShowHistory();

        Assert.False(vm.IsEmptyHistoryVisible);
    }

    // ── SelectCurrentItem — category level ───────────────────────────────

    [Fact]
    public void SelectCurrentItem_AtCategoryLevel_NavigatesToFormatLevel()
    {
        var vm = CreateVm();
        vm.ShowCategories();

        vm.SelectCurrentItem();

        Assert.True(vm.IsFormatLevel);
    }

    [Fact]
    public void SelectCurrentItem_AtCategoryLevel_ReturnsFalse()
    {
        var vm = CreateVm();
        vm.ShowCategories();

        bool result = vm.SelectCurrentItem();

        Assert.False(result);
    }

    // ── SelectCurrentItem — format level ─────────────────────────────────

    [Fact]
    public void SelectCurrentItem_AtFormatLevel_RaisesFormatSelected()
    {
        var vm = CreateVm();
        vm.ShowCategories();
        vm.SelectCurrentItem(); // navigate into Email formats

        string? receivedCategory = null;
        string? receivedFormat = null;
        vm.FormatSelected += (cat, fmt) => { receivedCategory = cat; receivedFormat = fmt; };
        vm.SelectCurrentItem();

        Assert.Equal("Email", receivedCategory);
        Assert.Equal("email-basic", receivedFormat);
    }

    [Fact]
    public void SelectCurrentItem_AtFormatLevel_ClosesAndReturnsTrue()
    {
        var vm = CreateVm();
        vm.ShowCategories();
        vm.SelectCurrentItem();
        bool closeCalled = false;
        vm.CloseRequested += () => closeCalled = true;

        bool result = vm.SelectCurrentItem();

        Assert.True(closeCalled);
        Assert.True(result);
    }

    // ── SelectCurrentItem — history level ────────────────────────────────

    [Fact]
    public void SelectCurrentItem_AtHistoryLevel_RaisesHistoryItemSelected()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("hello", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);
        vm.ShowHistory();
        string? received = null;
        vm.HistoryItemSelected += val => received = val;

        vm.SelectCurrentItem();

        Assert.Equal("hello", received);
    }

    [Fact]
    public void SelectCurrentItem_AtHistoryLevel_RaisesCloseRequested()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("hello", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);
        vm.ShowHistory();
        bool closeCalled = false;
        vm.CloseRequested += () => closeCalled = true;

        vm.SelectCurrentItem();

        Assert.True(closeCalled);
    }

    // ── HandleMnemonic ────────────────────────────────────────────────────

    [Fact]
    public void HandleMnemonic_MatchingKey_NavigatesToFormats()
    {
        var vm = CreateVm();
        vm.ShowCategories();

        vm.HandleMnemonic("E");

        Assert.True(vm.IsFormatLevel);
    }

    [Fact]
    public void HandleMnemonic_MatchingKey_ReturnsTrue()
    {
        var vm = CreateVm();
        vm.ShowCategories();

        bool result = vm.HandleMnemonic("E");

        Assert.True(result);
    }

    [Fact]
    public void HandleMnemonic_IsCaseInsensitive()
    {
        var vm = CreateVm();
        vm.ShowCategories();

        bool result = vm.HandleMnemonic("e");

        Assert.True(result);
    }

    [Fact]
    public void HandleMnemonic_NonMatchingKey_ReturnsFalse()
    {
        var vm = CreateVm();
        vm.ShowCategories();

        bool result = vm.HandleMnemonic("Z");

        Assert.False(result);
    }

    [Fact]
    public void HandleMnemonic_MultiCharKey_ReturnsFalse()
    {
        var vm = CreateVm();
        vm.ShowCategories();

        bool result = vm.HandleMnemonic("Tab");

        Assert.False(result);
    }

    [Fact]
    public void HandleMnemonic_AtFormatLevel_ReturnsFalse()
    {
        var vm = CreateVm();
        vm.ShowFormats(new StubGenerator("Email", "E", 1, Format("id", "name")));

        // format level items are MenuItemViewModel but no mnemonic match returns false
        bool result = vm.HandleMnemonic("E");

        Assert.False(result);
    }

    // ── PropertyChanged ───────────────────────────────────────────────────

    [Fact]
    public void ShowCategories_RaisesPropertyChangedForHeaderText()
    {
        var vm = CreateVm();
        vm.ShowFormats(new StubGenerator("Email", "E", 1, Format("id", "name")));
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.ShowCategories();

        Assert.Contains(nameof(PopupViewModel.HeaderText), changed);
    }

    [Fact]
    public void ShowHistory_WhenHasEntries_RaisesPropertyChangedForItems()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("v", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.ShowHistory();

        Assert.Contains(nameof(PopupViewModel.Items), changed);
    }

    // ── DeleteHistoryItem ─────────────────────────────────────────────────

    [Fact]
    public void DeleteHistoryItem_RemovesItemFromList()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("a", "Email", "Basic", DateTime.Now));
        history.Add(new HistoryEntry("b", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);
        vm.ShowHistory();

        vm.DeleteHistoryItem("a");

        Assert.Single(vm.Items);
        var remaining = Assert.IsType<HistoryItemViewModel>(vm.Items[0]);
        Assert.Equal("b", remaining.Value);
    }

    [Fact]
    public void DeleteHistoryItem_LastItem_ShowsEmptyHistoryState()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("a", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);
        vm.ShowHistory();

        vm.DeleteHistoryItem("a");

        Assert.True(vm.IsEmptyHistoryVisible);
        Assert.Empty(vm.Items);
    }

    [Fact]
    public void DeleteHistoryItem_LastItem_SetsSelectedIndexToMinusOne()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("a", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);
        vm.ShowHistory();

        vm.DeleteHistoryItem("a");

        Assert.Equal(-1, vm.SelectedIndex);
    }

    [Fact]
    public void DeleteHistoryItem_NotLastItem_SelectedIndexClampsToNewCount()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("a", "Email", "Basic", DateTime.Now));
        history.Add(new HistoryEntry("b", "Email", "Basic", DateTime.Now));
        history.Add(new HistoryEntry("c", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);
        vm.ShowHistory();
        vm.SelectedIndex = 2; // select last item

        vm.DeleteHistoryItem("c");

        Assert.Equal(1, vm.SelectedIndex); // clamped to new last index
    }

    [Fact]
    public void DeleteHistoryItem_RaisesPropertyChangedForItems()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("a", "Email", "Basic", DateTime.Now));
        history.Add(new HistoryEntry("b", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);
        vm.ShowHistory();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.DeleteHistoryItem("a");

        Assert.Contains(nameof(PopupViewModel.Items), changed);
    }

    // ── DeleteSelectedHistoryItem ─────────────────────────────────────────

    [Fact]
    public void DeleteSelectedHistoryItem_RemovesSelectedEntry()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("a", "Email", "Basic", DateTime.Now));
        history.Add(new HistoryEntry("b", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);
        vm.ShowHistory();
        vm.SelectedIndex = 0; // "b" is first (most recent)

        vm.DeleteSelectedHistoryItem();

        Assert.Single(vm.Items);
        var remaining = Assert.IsType<HistoryItemViewModel>(vm.Items[0]);
        Assert.Equal("a", remaining.Value);
    }

    [Fact]
    public void DeleteSelectedHistoryItem_WhenNoSelection_DoesNotThrow()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("a", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);
        vm.ShowHistory();
        vm.SelectedIndex = -1;

        var ex = Record.Exception(() => vm.DeleteSelectedHistoryItem());

        Assert.Null(ex);
    }

    [Fact]
    public void DeleteSelectedHistoryItem_PreservesSelectionOnRemainingItems()
    {
        var history = new HistoryService();
        history.Add(new HistoryEntry("a", "Email", "Basic", DateTime.Now));
        history.Add(new HistoryEntry("b", "Email", "Basic", DateTime.Now));
        history.Add(new HistoryEntry("c", "Email", "Basic", DateTime.Now));
        var vm = CreateVm(history: history);
        vm.ShowHistory();
        vm.SelectedIndex = 0; // delete first item

        vm.DeleteSelectedHistoryItem();

        Assert.Equal(0, vm.SelectedIndex);
    }
}
