using MockPaste.Core.Models;

namespace MockPaste.Infrastructure;

public sealed class HistoryService
{
    private readonly LinkedList<HistoryEntry> _entries = new();
    private int _maxSize;

    public HistoryService(int maxSize = 10) => _maxSize = maxSize;

    public void UpdateMaxSize(int maxSize)
    {
        _maxSize = maxSize;
        while (_entries.Count > _maxSize)
            _entries.RemoveLast();
    }

    public void Add(HistoryEntry entry)
    {
        var existing = _entries.FirstOrDefault(e => e.Value == entry.Value);
        if (existing is not null)
            _entries.Remove(existing);

        _entries.AddFirst(entry);

        while (_entries.Count > _maxSize)
            _entries.RemoveLast();
    }

    public void Promote(string value)
    {
        var existing = _entries.FirstOrDefault(e => e.Value == value);
        if (existing is null) return;

        _entries.Remove(existing);
        _entries.AddFirst(existing with { GeneratedAt = DateTime.Now });
    }

    public IReadOnlyList<HistoryEntry> GetAll() => _entries.ToList();
}
