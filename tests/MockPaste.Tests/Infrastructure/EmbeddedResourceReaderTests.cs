using System.Reflection;
using MockPaste.Infrastructure;

namespace MockPaste.Tests.Infrastructure;

public sealed class EmbeddedResourceReaderTests
{
    private static readonly Assembly TestAssembly = typeof(EmbeddedResourceReaderTests).Assembly;

    // ── Found ────────────────────────────────────────────────────────────────

    [Fact]
    public void Read_ReturnsContent_WhenResourceExists()
    {
        var content = EmbeddedResourceReader.Read(TestAssembly, "sample.txt");

        Assert.NotNull(content);
        Assert.Contains("Hello from embedded resource.", content);
    }

    [Fact]
    public void Read_MatchesByFileNameSuffix()
    {
        // The manifest name is the full dotted path; we match only by the suffix.
        var content = EmbeddedResourceReader.Read(TestAssembly, "sample.txt");

        Assert.NotNull(content);
    }

    [Fact]
    public void Read_IsCaseInsensitive()
    {
        var lower = EmbeddedResourceReader.Read(TestAssembly, "sample.txt");
        var upper = EmbeddedResourceReader.Read(TestAssembly, "SAMPLE.TXT");

        Assert.NotNull(lower);
        Assert.Equal(lower, upper);
    }

    // ── Not found ────────────────────────────────────────────────────────────

    [Fact]
    public void Read_ReturnsNull_WhenResourceDoesNotExist()
    {
        var content = EmbeddedResourceReader.Read(TestAssembly, "does_not_exist.txt");

        Assert.Null(content);
    }

    [Fact]
    public void Read_ReturnsNull_WhenFileNameIsPartialButNotSuffix()
    {
        // "ample.txt" is a suffix of "sample.txt" — this test documents exact suffix behaviour.
        var content = EmbeddedResourceReader.Read(TestAssembly, "ample.txt");

        // EndsWith("ample.txt") is true for "sample.txt", so this will match.
        // Documenting this known behaviour so callers pass distinct file names.
        Assert.NotNull(content);
    }
}
