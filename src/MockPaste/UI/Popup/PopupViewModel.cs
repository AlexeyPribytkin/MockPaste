using System.ComponentModel;
using System.Runtime.CompilerServices;
using MockPaste.Core.Generators;
using MockPaste.Infrastructure;
using MockPaste.UI;

namespace MockPaste.UI.Popup;

/// <summary>
/// Owns popup navigation state and item building, independent of WPF window APIs.
/// </summary>
public sealed class PopupViewModel : INotifyPropertyChanged
{
    private enum PopupLevel { Categories, Formats, History }

    private readonly GeneratorRegistry _generators;
    private readonly HistoryService _history;
    private readonly Func<string, string> _resourceResolver;

    private PopupLevel _level;
    private string _headerText = string.Empty;
    private bool _isBackButton;
    private bool _isHistoryButtonVisible = true;
    private bool _isEmptyHistoryVisible;
    private IReadOnlyList<IPopupItem> _items = [];
    private int _selectedIndex = -1;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised when a generator format is chosen. Args: (categoryName, formatId).</summary>
    public event Action<string, string>? FormatSelected;

    /// <summary>Raised when a history entry is chosen. Arg: value.</summary>
    public event Action<string>? HistoryItemSelected;

    /// <summary>Raised when the popup should close (before firing a selection event).</summary>
    public event Action? CloseRequested;

    public PopupViewModel(GeneratorRegistry generators, HistoryService history)
    {
        _generators = generators;
        _history = history;
        _resourceResolver = key => System.Windows.Application.Current?.Resources[key] as string ?? key;
        _headerText = Res("StringAppName");
    }

    // ── Navigation state ─────────────────────────────────────────────────

    /// <summary><c>true</c> when the popup is showing the format sub-menu for a specific category.</summary>
    public bool IsFormatLevel => _level == PopupLevel.Formats;

    /// <summary><c>true</c> when the popup is showing the history list.</summary>
    public bool IsHistoryLevel => _level == PopupLevel.History;

    /// <summary>Title text shown at the top of the popup (e.g. "MockPaste", "← GUID", "← History").</summary>
    public string HeaderText
    {
        get => _headerText;
        private set => SetField(ref _headerText, value);
    }

    /// <summary>True when header acts as a back-navigation button.</summary>
    public bool IsBackButton
    {
        get => _isBackButton;
        private set => SetField(ref _isBackButton, value);
    }

    /// <summary>Controls visibility of the history navigation button; hidden when already on the history or format level.</summary>
    public bool IsHistoryButtonVisible
    {
        get => _isHistoryButtonVisible;
        private set => SetField(ref _isHistoryButtonVisible, value);
    }

    /// <summary>When <c>true</c>, an empty-state placeholder is shown instead of the list (history level only).</summary>
    public bool IsEmptyHistoryVisible
    {
        get => _isEmptyHistoryVisible;
        private set => SetField(ref _isEmptyHistoryVisible, value);
    }

    /// <summary>The list of items currently shown in the popup (categories, formats, or history entries).</summary>
    public IReadOnlyList<IPopupItem> Items
    {
        get => _items;
        private set => SetField(ref _items, value);
    }

    /// <summary>Zero-based index of the currently highlighted item; -1 when nothing is selected.</summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetField(ref _selectedIndex, value);
    }

    // ── Navigation ───────────────────────────────────────────────────────

    // ── Navigation ───────────────────────────────────────────────────

    /// <summary>Navigates to the top-level category list and resets the header and button visibility.</summary>
    public void ShowCategories()
    {
        SetLevel(PopupLevel.Categories);
        HeaderText = Res("StringAppName");
        IsBackButton = false;
        IsHistoryButtonVisible = true;
        IsEmptyHistoryVisible = false;

        Items = _generators.GetAll().Select(g => new MenuItemViewModel
        {
            DisplayName = g.CategoryName,
            MnemonicKey = g.MnemonicKey,
            CategoryName = g.CategoryName,
            HasSubMenu = true
        }).ToList();

        SelectedIndex = Items.Count > 0 ? 0 : -1;
    }

    /// <summary>Navigates to the format sub-menu for <paramref name="generator"/> and updates the header to show the back-navigation title.</summary>
    public void ShowFormats(IFakeDataGenerator generator)
    {
        SetLevel(PopupLevel.Formats);
        HeaderText = string.Format(Res("StringPopupBackFormat"), generator.CategoryName);
        IsBackButton = true;
        IsHistoryButtonVisible = false;
        IsEmptyHistoryVisible = false;

        Items = generator.SupportedFormats.Select(f => new MenuItemViewModel
        {
            DisplayName = f.Name,
            Description = f.Description,
            CategoryName = generator.CategoryName,
            FormatId = f.FormatId,
            HasSubMenu = false
        }).ToList();

        SelectedIndex = Items.Count > 0 ? 0 : -1;
    }

    /// <summary>Navigates to the history list view, refreshing entries from the <see cref="HistoryService"/>.</summary>
    public void ShowHistory()
    {
        SetLevel(PopupLevel.History);
        HeaderText = string.Format(Res("StringPopupBackFormat"), Res("StringButtonHistory"));
        IsBackButton = true;
        IsHistoryButtonVisible = false;

        RefreshHistoryItems();

        SelectedIndex = Items.Count > 0 ? 0 : -1;
    }

    /// <summary>Removes a history entry by value and refreshes the history list.</summary>
    public void DeleteHistoryItem(string value)
    {
        // Capture before RefreshHistoryItems() replaces Items, which resets
        // SelectedIndex to -1 via the TwoWay binding on the ListBox.
        int indexBeforeDelete = _selectedIndex;
        _history.Remove(value);
        RefreshHistoryItems();
        SelectedIndex = Items.Count > 0 ? Math.Min(indexBeforeDelete, Items.Count - 1) : -1;
    }

    /// <summary>Deletes the currently selected history item.</summary>
    public void DeleteSelectedHistoryItem()
    {
        if (_selectedIndex < 0 || _selectedIndex >= Items.Count)
        {
            return;
        }

        if (Items[_selectedIndex] is HistoryItemViewModel item)
        {
            DeleteHistoryItem(item.Value);
        }
    }

    /// <summary>Rebuilds the <see cref="Items"/> list from the current <see cref="HistoryService"/> snapshot.</summary>
    private void RefreshHistoryItems()
    {
        var entries = _history.GetAll();
        if (entries.Count == 0)
        {
            Items = [];
            IsEmptyHistoryVisible = true;
        }
        else
        {
            IsEmptyHistoryVisible = false;
            Items = entries.Select(e => new HistoryItemViewModel
            {
                Value = e.Value,
                CategoryName = e.CategoryName,
                FormatName = e.FormatName,
                DeleteCommand = new RelayCommand(() => DeleteHistoryItem(e.Value))
            }).ToList();
        }
    }

    // ── Selection ────────────────────────────────────────────────────────

    /// <summary>
    /// Handles selection of the current item.
    /// Raises <see cref="CloseRequested"/> and the appropriate selection event when a leaf item is chosen.
    /// Returns true if the popup should close.
    /// </summary>
    public bool SelectCurrentItem()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Items.Count)
        {
            return false;
        }

        if (Items[SelectedIndex] is HistoryItemViewModel histItem)
        {
            CloseRequested?.Invoke();
            HistoryItemSelected?.Invoke(histItem.Value);
            return true;
        }

        if (Items[SelectedIndex] is not MenuItemViewModel item)
        {
            return false;
        }

        if (_level != PopupLevel.Formats)
        {
            var gen = _generators.Get(item.CategoryName);
            if (gen is not null)
            {
                ShowFormats(gen);
            }
        }
        else
        {
            CloseRequested?.Invoke();
            FormatSelected?.Invoke(item.CategoryName, item.FormatId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles keyboard mnemonic navigation on the category level.
    /// Returns true if a match was found and acted upon.
    /// </summary>
    /// <summary>
    /// Handles keyboard mnemonic navigation on the category level.
    /// Returns <c>true</c> if a match was found and acted upon.
    /// </summary>
    public bool HandleMnemonic(string keyChar)
    {
        if (keyChar.Length != 1)
        {
            return false;
        }

        for (int i = 0; i < Items.Count; i++)
        {
            if (Items[i] is MenuItemViewModel item &&
                item.MnemonicKey.Equals(keyChar, StringComparison.OrdinalIgnoreCase))
            {
                SelectedIndex = i;
                SelectCurrentItem();
                return true;
            }
        }

        return false;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>Updates the internal navigation level and notifies dependent computed properties.</summary>
    private void SetLevel(PopupLevel level)
    {
        if (_level == level)
        {
            return;
        }

        _level = level;
        Notify(nameof(IsFormatLevel));
        Notify(nameof(IsHistoryLevel));
    }

    /// <summary>Sets <paramref name="field"/> and fires <see cref="PropertyChanged"/> if the value changed.</summary>
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        Notify(name);
        return true;
    }

    private void Notify(string? name)
    {
        if (name is null)
        {
            return;
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private string Res(string key) => _resourceResolver(key);
}
