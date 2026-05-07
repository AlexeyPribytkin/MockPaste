using System.IO;
using System.Reflection;

namespace MockPaste.Infrastructure;

/// <summary>
/// Reads text content from assembly embedded resources.
/// </summary>
public static class EmbeddedResourceReader
{
    /// <summary>
    /// Returns the text content of an embedded resource whose manifest name ends with
    /// <paramref name="fileName"/>, or <see langword="null"/> if no matching resource is found.
    /// </summary>
    /// <param name="assembly">Assembly to search.</param>
    /// <param name="fileName">File name suffix to match (e.g. <c>"LICENSE"</c> or <c>"THIRD_PARTY_NOTICES.md"</c>).</param>
    public static string? Read(Assembly assembly, string fileName)
    {
        var resourceName = Array.Find(
            assembly.GetManifestResourceNames(),
            n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
