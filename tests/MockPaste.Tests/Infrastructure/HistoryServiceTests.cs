using MockPaste.Core.Models;
using MockPaste.Infrastructure;

namespace MockPaste.Tests.Infrastructure;

public sealed class HistoryServiceTests
{
    // ── Add ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Add_NewEntry_AppearsAtFront()
    {
        var svc = new HistoryService(10);
        svc.Add(Entry("a"));
        Assert.Equal("a", svc.GetAll()[0].Value);
    }

    [Fact]
    public void Add_MultipleEntries_MostRecentIsFirst()
    {
        var svc = new HistoryService(10);
        svc.Add(Entry("first"));
        svc.Add(Entry("second"));
        Assert.Equal("second", svc.GetAll()[0].Value);
        Assert.Equal("first",  svc.GetAll()[1].Value);
    }

    [Fact]
    public void Add_DuplicateValue_MovesToFront()
    {
        var svc = new HistoryService(10);
        svc.Add(Entry("a"));
        svc.Add(Entry("b"));
        svc.Add(Entry("a"));   // duplicate — should rise to top
        var all = svc.GetAll();
        Assert.Equal("a", all[0].Value);
        Assert.Equal(2, all.Count);   // no duplicate in list
    }

    [Fact]
    public void Add_ExceedsMaxSize_OldestEntryDropped()
    {
        var svc = new HistoryService(3);
        svc.Add(Entry("a"));
        svc.Add(Entry("b"));
        svc.Add(Entry("c"));
        svc.Add(Entry("d"));
        var all = svc.GetAll();
        Assert.Equal(3, all.Count);
        Assert.DoesNotContain(all, e => e.Value == "a");
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetAll_EmptyService_ReturnsEmptyList()
    {
        var svc = new HistoryService(10);
        Assert.Empty(svc.GetAll());
    }

    [Fact]
    public void GetAll_ReturnsSnapshot_NotLiveReference()
    {
        var svc = new HistoryService(10);
        svc.Add(Entry("a"));
        var snapshot = svc.GetAll();
        svc.Add(Entry("b"));
        // snapshot taken before the second Add should still have only 1 item
        Assert.Single(snapshot);
    }

    // ── Promote ──────────────────────────────────────────────────────────────

    [Fact]
    public void Promote_ExistingValue_MovesToFront()
    {
        var svc = new HistoryService(10);
        svc.Add(Entry("a"));
        svc.Add(Entry("b"));
        svc.Add(Entry("c"));
        svc.Promote("a");
        Assert.Equal("a", svc.GetAll()[0].Value);
    }

    [Fact]
    public void Promote_UnknownValue_DoesNotThrow()
    {
        var svc = new HistoryService(10);
        svc.Add(Entry("a"));
        var ex = Record.Exception(() => svc.Promote("does-not-exist"));
        Assert.Null(ex);
        Assert.Single(svc.GetAll());
    }

    [Fact]
    public void Promote_PreservesGeneratedAt()
    {
        var svc = new HistoryService(10);
        var original = Entry("a");
        svc.Add(original);
        svc.Promote("a");
        var promoted = svc.GetAll()[0];
        Assert.Equal(original.GeneratedAt, promoted.GeneratedAt);
    }

    // ── UpdateMaxSize ─────────────────────────────────────────────────────────

    [Fact]
    public void UpdateMaxSize_TrimsExcessEntries()
    {
        var svc = new HistoryService(5);
        for (int i = 0; i < 5; i++) svc.Add(Entry(i.ToString()));
        svc.UpdateMaxSize(2);
        Assert.Equal(2, svc.GetAll().Count);
    }

    [Fact]
    public void UpdateMaxSize_Increase_DoesNotDropEntries()
    {
        var svc = new HistoryService(3);
        svc.Add(Entry("a"));
        svc.Add(Entry("b"));
        svc.UpdateMaxSize(10);
        Assert.Equal(2, svc.GetAll().Count);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static HistoryEntry Entry(string value) =>
        new(value, "Test", "TestFormat", DateTime.Now);
}
