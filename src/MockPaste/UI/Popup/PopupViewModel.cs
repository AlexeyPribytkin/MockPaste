using System.Collections;
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
    private readonly GeneratorRegistry _generators;
    private readonly HistoryService _history;

    private bool _isFormatLevel;
    private bool _isHistoryLevel;
    private string _headerText = "MockPaste";
    private bool _isBackButton;
    private bool _isHistoryButtonVisible = true;
    private bool _isEmptyHistoryVisible;
    private IList? _items;
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

    public bool IsFormatLevel => _isFormatLevel;
    public bool IsHistoryLevel => _isHistoryLevel;

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

    public IList? Items
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
        _isFormatLevel = false;
        _isHistoryLevel = false;
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
        _isFormatLevel = true;
        _isHistoryLevel = false;
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
        _isHistoryLevel = true;
        _isFormatLevel = false;
        HeaderText = "← History";
        IsBackButton = true;
        IsHistoryButtonVisible = false;

        var entries = _history.GetAll();
        if (entries.Count == 0)
        {
            Items = null;
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

        SelectedIndex = (Items?.Count ?? 0) > 0 ? 0 : -1;
    }

    // ── Selection ────────────────────────────────────────────────────────

    /// <summary>
    /// Handles selection of the current item.
    /// Raises <see cref="CloseRequested"/> and the appropriate selection event when a leaf item is chosen.
    /// Returns true if the popup should close.
    /// </summary>
    public bool SelectCurrentItem()
    {
        if (SelectedIndex < 0 || Items is null)
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

        if (!_isFormatLevel)
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

        if (Items is not IEnumerable<MenuItemViewModel> items)
        {
            return false;
        }

        var match = items.FirstOrDefault(i =>
            i.MnemonicKey.Equals(keyChar, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            return false;
        }

        SelectedIndex = Items.IndexOf(match);
        SelectCurrentItem();
        return true;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
