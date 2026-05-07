using System.Windows;
using System.Windows.Documents;
using MockPaste.UI.Helpers;

namespace MockPaste.Tests.UI.Settings;

/// <summary>
/// Tests for <see cref="MarkdownFlowFormatter"/>.
/// Each test exercises one Markdown construct in isolation.
/// </summary>
public sealed class MarkdownFlowFormatterTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FlowDocument Format(string markdown)
    {
        var doc = new FlowDocument();
        MarkdownFlowFormatter.AppendTo(doc, markdown);
        return doc;
    }

    private static Paragraph SingleParagraph(string markdown)
    {
        var doc = Format(markdown);
        var block = Assert.Single(doc.Blocks);
        return Assert.IsType<Paragraph>(block);
    }

    // ── H1 ───────────────────────────────────────────────────────────────────

    [Fact]
    public void H1_ProducesSingleParagraph()
    {
        var para = SingleParagraph("# Title");

        Assert.Equal("Title", ((Run)para.Inlines.FirstInline).Text);
    }

    [Fact]
    public void H1_HasCorrectFontSize()
    {
        var para = SingleParagraph("# Title");

        Assert.Equal(15, para.FontSize);
    }

    [Fact]
    public void H1_IsBold()
    {
        var para = SingleParagraph("# Title");

        Assert.Equal(FontWeights.Bold, para.FontWeight);
    }

    // ── H2 ───────────────────────────────────────────────────────────────────

    [Fact]
    public void H2_ProducesSingleParagraph()
    {
        var para = SingleParagraph("## Subtitle");

        Assert.Equal("Subtitle", ((Run)para.Inlines.FirstInline).Text);
    }

    [Fact]
    public void H2_HasCorrectFontSize()
    {
        var para = SingleParagraph("## Subtitle");

        Assert.Equal(13, para.FontSize);
    }

    [Fact]
    public void H2_IsSemiBold()
    {
        var para = SingleParagraph("## Subtitle");

        Assert.Equal(FontWeights.SemiBold, para.FontWeight);
    }

    // ── H3 ───────────────────────────────────────────────────────────────────

    [Fact]
    public void H3_HasCorrectFontSize()
    {
        var para = SingleParagraph("### Sub-subtitle");

        Assert.Equal(12, para.FontSize);
    }

    [Fact]
    public void H3_IsSemiBold()
    {
        var para = SingleParagraph("### Sub-subtitle");

        Assert.Equal(FontWeights.SemiBold, para.FontWeight);
    }

    // ── Bullets ───────────────────────────────────────────────────────────────

    [Fact]
    public void SingleBullet_ProducesOneListWithOneItem()
    {
        var doc = Format("- Item A");

        var list = Assert.IsType<List>(Assert.Single(doc.Blocks));
        Assert.Single(list.ListItems);
    }

    [Fact]
    public void SingleBullet_HasDiscMarker()
    {
        var doc = Format("- Item A");

        var list = Assert.IsType<List>(Assert.Single(doc.Blocks));
        Assert.Equal(TextMarkerStyle.Disc, list.MarkerStyle);
    }

    [Fact]
    public void ConsecutiveBullets_AreGroupedIntoOneList()
    {
        var doc = Format("- Alpha\n- Beta\n- Gamma");

        var list = Assert.IsType<List>(Assert.Single(doc.Blocks));
        Assert.Equal(3, list.ListItems.Count);
    }

    [Fact]
    public void BulletsSeparatedByBlankLine_ProduceTwoLists()
    {
        var doc = Format("- A\n\n- B");

        var blocks = doc.Blocks.ToList();
        Assert.Equal(3, blocks.Count); // list, spacer, list
        Assert.IsType<List>(blocks[0]);
        Assert.IsType<Paragraph>(blocks[1]);
        Assert.IsType<List>(blocks[2]);
    }

    [Fact]
    public void BulletItem_TextIsPreservedWithoutPrefix()
    {
        var doc = Format("- Hello world");

        var list = Assert.IsType<List>(Assert.Single(doc.Blocks));
        var item = Assert.Single(list.ListItems);
        var para = Assert.IsType<Paragraph>(Assert.Single(item.Blocks));
        var run = Assert.IsType<Run>(Assert.Single(para.Inlines));
        Assert.Equal("Hello world", run.Text);
    }

    // ── Blank lines ──────────────────────────────────────────────────────────

    [Fact]
    public void BlankLine_ProducesEmptyParagraph()
    {
        var doc = Format("\n");

        // Two blocks: the blank line produces a Paragraph (the empty \n is the last item)
        foreach (var block in doc.Blocks)
        {
            var para = Assert.IsType<Paragraph>(block);
            Assert.Empty(para.Inlines);
        }
    }

    // ── Plain text ───────────────────────────────────────────────────────────

    [Fact]
    public void PlainLine_ProducesParagraphWithText()
    {
        var para = SingleParagraph("Just some text.");

        var run = Assert.IsType<Run>(Assert.Single(para.Inlines));
        Assert.Equal("Just some text.", run.Text);
    }

    // ── Line ending normalization ─────────────────────────────────────────────

    [Fact]
    public void CrLfLineEndings_AreNormalized()
    {
        var doc = Format("# Title\r\nplain text");

        Assert.Equal(2, doc.Blocks.Count);
    }

    [Fact]
    public void CrLineEndings_AreNormalized()
    {
        var doc = Format("# Title\rplain text");

        Assert.Equal(2, doc.Blocks.Count);
    }

    // ── Mixed document ───────────────────────────────────────────────────────

    [Fact]
    public void MixedContent_ProducesBlocksInOrder()
    {
        const string markdown = """
            # H1
            ## H2
            ### H3
            - item
            plain
            """;

        var blocks = Format(markdown).Blocks.ToList();

        Assert.IsType<Paragraph>(blocks[0]); // H1
        Assert.IsType<Paragraph>(blocks[1]); // H2
        Assert.IsType<Paragraph>(blocks[2]); // H3
        Assert.IsType<List>(blocks[3]);      // bullet
        Assert.IsType<Paragraph>(blocks[4]); // plain
    }
}
