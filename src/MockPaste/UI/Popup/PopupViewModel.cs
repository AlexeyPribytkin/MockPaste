using System.ComponentModel;
using System.Runtime.CompilerServices;
using MockPaste.Core.Generators;
using MockPaste.Infrastructure;

namespace MockPaste.UI.Popup;

/// <summary>
/// Owns popup navigation state and item building, independent of WPF window APIs.
/// </summary>
public sealed class PopupViewModel : INotifyPropertyChanged
{
    private enum PopupLevel { Categories, Formats, History }

    private readonly GeneratorRegistry _generators;
    private readonly HistoryService _history;

    private PopupLevel _level;
    private string _headerText = "MockPaste";
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
    }

    // ── Navigation state ─────────────────────────────────────────────────

    public bool IsFormatLevel => _level == PopupLevel.Formats;
    public bool IsHistoryLevel => _level == PopupLevel.History;

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

    public bool IsHistoryButtonVisible
    {
        get => _isHistoryButtonVisible;
        private set => SetField(ref _isHistoryButtonVisible, value);
    }

    public bool IsEmptyHistoryVisible
    {
        get => _isEmptyHistoryVisible;
        private set => SetField(ref _isEmptyHistoryVisible, value);
    }

    public IReadOnlyList<IPopupItem> Items
    {
        get => _items;
        private set => SetField(ref _items, value);
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetField(ref _selectedIndex, value);
    }

    // ── Navigation ───────────────────────────────────────────────────────

    public void ShowCategories()
    {
        SetLevel(PopupLevel.Categories);
        HeaderText = "MockPaste";
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

    public void ShowFormats(IFakeDataGenerator generator)
    {
        SetLevel(PopupLevel.Formats);
        HeaderText = $"← {generator.CategoryName}";
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

    public void ShowHistory()
    {
        SetLevel(PopupLevel.History);
        HeaderText = "← History";
        IsBackButton = true;
        IsHistoryButtonVisible = false;

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
                FormatName = e.FormatName
            }).ToList();
        }

        SelectedIndex = Items.Count > 0 ? 0 : -1;
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
}
