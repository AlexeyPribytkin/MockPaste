using System.IO;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MockPaste.UI.Helpers;

/// <summary>
/// Converts a limited subset of Markdown into a WPF <see cref="FlowDocument"/>.
/// Supported syntax: # H1, ## H2, ### H3, - bullet list, plain paragraphs, blank lines.
/// </summary>
public static class MarkdownFlowFormatter
{
    /// <summary>
    /// Appends the formatted content of <paramref name="text"/> into <paramref name="document"/>.
    /// </summary>
    /// <param name="document">Target document to append blocks into.</param>
    /// <param name="text">Markdown source text.</param>
    /// <param name="bodyFont">Font applied to plain paragraph lines. Pass <see langword="null"/> to inherit from the document.</param>
    public static void AppendTo(FlowDocument document, string text, FontFamily? bodyFont = null)
    {
        // Normalize line endings so splitting is consistent on all platforms.
        using var reader = new StringReader(text.Replace("\r\n", "\n").Replace('\r', '\n'));

        List? currentList = null;

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                currentList = null;
                document.Blocks.Add(Heading(line[4..], fontSize: 12, topMargin: 6));
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                currentList = null;
                document.Blocks.Add(Heading(line[3..], fontSize: 13, topMargin: 8));
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                currentList = null;
                document.Blocks.Add(Heading(line[2..], fontSize: 15, topMargin: 10, bold: true));
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                // Accumulate consecutive bullet lines into one WPF List.
                if (currentList is null)
                {
                    currentList = new List
                    {
                        MarkerStyle = TextMarkerStyle.Disc,
                        Margin = new Thickness(16, 2, 0, 2),
                        Padding = new Thickness(4, 0, 0, 0)
                    };
                    document.Blocks.Add(currentList);
                }

                currentList.ListItems.Add(new ListItem(new Paragraph(new Run(line[2..]))
                {
                    Margin = new Thickness(0, 1, 0, 1)
                }));
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                currentList = null;
                // Empty Paragraph (no Run) acts as a visual spacer without extra alloc.
                document.Blocks.Add(new Paragraph { Margin = new Thickness(0, 2, 0, 2) });
            }
            else
            {
                currentList = null;
                var para = new Paragraph(new Run(line))
                {
                    Margin = new Thickness(0, 1, 0, 1)
                };
                if (bodyFont is not null)
                {
                    para.FontFamily = bodyFont;
                }
                document.Blocks.Add(para);
            }
        }
    }

    private static Paragraph Heading(string text, double fontSize, double topMargin, bool bold = false) =>
        new(new Run(text))
        {
            FontSize = fontSize,
            FontWeight = bold ? FontWeights.Bold : FontWeights.SemiBold,
            Margin = new Thickness(0, topMargin, 0, 2)
        };
}
