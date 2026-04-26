using MockPaste.Core.Models;

namespace MockPaste.Infrastructure;

/// <summary>
/// Thread-safe, bounded MRU (most-recently-used) list of <see cref="HistoryEntry"/> items.
/// Duplicate values are promoted to the front rather than duplicated.
/// Oldest entries are evicted automatically when the list exceeds <c>maxSize</c>.
/// </summary>
public sealed class HistoryService
{
    private readonly LinkedList<HistoryEntry> _entries = new();
    private readonly Lock _lock = new();
    private int _maxSize;

    /// <summary>Initializes a new history list with the given maximum capacity.</summary>
    /// <param name="maxSize">Maximum number of entries to retain. Defaults to 10.</param>
    public HistoryService(int maxSize = 10) => _maxSize = maxSize;

    /// <summary>
    /// Updates the maximum number of entries. If the current list exceeds the new
    /// limit, the oldest entries are removed immediately.
    /// </summary>
    public void UpdateMaxSize(int maxSize)
    {
        lock (_lock)
        {
            _maxSize = maxSize;
            while (_entries.Count > _maxSize)
            {
                _entries.RemoveLast();
            }
        }
    }

    /// <summary>
    /// Adds <paramref name="entry"/> to the front of the list. If an entry with the same
    /// <see cref="HistoryEntry.Value"/> already exists it is moved to the front instead
    /// of being duplicated. The oldest entry is discarded if the list is at capacity.
    /// </summary>
    public void Add(HistoryEntry entry)
    {
        lock (_lock)
        {
            var existing = _entries.FirstOrDefault(e => e.Value == entry.Value);
            if (existing is not null)
                _entries.Remove(existing);

            _entries.AddFirst(entry);

            while (_entries.Count > _maxSize)
                _entries.RemoveLast();
        }
    }

    /// <summary>
    /// Moves the entry whose <see cref="HistoryEntry.Value"/> matches <paramref name="value"/>
    /// to the front of the list. No-op when no matching entry is found.
    /// </summary>
    public void Promote(string value)
    {
        lock (_lock)
        {
            var existing = _entries.FirstOrDefault(e => e.Value == value);
            if (existing is null) return;

            _entries.Remove(existing);
            _entries.AddFirst(existing);
        }
    }

    /// <summary>
    /// Removes the entry whose <see cref="HistoryEntry.Value"/> matches <paramref name="value"/>.
    /// </summary>
    /// <returns><c>true</c> if an entry was found and removed; <c>false</c> if not found.</returns>
    public bool Remove(string value)
    {
        lock (_lock)
        {
            var existing = _entries.FirstOrDefault(e => e.Value == value);
            if (existing is null)
            {
                return false;
            }

            _entries.Remove(existing);
            return true;
        }
    }

    /// <summary>Returns a snapshot of all current entries in MRU order (most recent first).</summary>
    public IReadOnlyList<HistoryEntry> GetAll()
    {
        lock (_lock)
        {
            return _entries.ToList();
        }
    }
}

