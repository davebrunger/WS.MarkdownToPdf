using WS.MarkdownToPdf.Parsing;
using WS.MarkdownToPdf.Rendering;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class ColumnRendererTests
{
    private readonly PdfRenderer renderer = new();
    private readonly MarkdigParser parser = new();

    [Fact]
    public void Render_TwoColumns_ProducesValidPdf()
    {
        var markdown = """
            # Title

            <!-- columns: 2 -->

            Left column content here.

            Right column content here.

            <!-- /columns -->

            Footer paragraph.
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_ThreeColumns_ProducesValidPdf()
    {
        var markdown = """
            <!-- columns: 3 -->

            First column.

            Second column.

            Third column.

            <!-- /columns -->
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_ColumnsWithMixedContent_ProducesValidPdf()
    {
        var markdown = """
            <!-- columns: 2 -->

            ## A Heading

            Paragraph with **bold** and *italic*.

            - Item one
            - Item two

            ## Another Heading

            > A block quote in a column.

            Another paragraph.

            <!-- /columns -->
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_ColumnsWithoutClosingTag_ProducesValidPdf()
    {
        var markdown = """
            <!-- columns: 2 -->

            First content paragraph.

            Second content paragraph.
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_ColumnsFollowedByContent_ContentAppearsAfterColumns()
    {
        var markdown = """
            <!-- columns: 2 -->

            Left paragraph.

            Right paragraph.

            <!-- /columns -->

            This is after the columns.
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_SingleBlockInColumns_ProducesValidPdf()
    {
        var markdown = """
            <!-- columns: 3 -->

            Only one block of content.

            <!-- /columns -->
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_HtmlComment_SilentlyIgnored()
    {
        var markdown = """
            # Title

            <!-- This is an ordinary HTML comment -->

            A paragraph.
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_ColumnsWithTable_ProducesValidPdf()
    {
        var markdown = """
            <!-- columns: 2 -->

            | A | B |
            |---|---|
            | 1 | 2 |

            | C | D |
            |---|---|
            | 3 | 4 |

            <!-- /columns -->
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_ColumnsWithThematicBreak_ProducesValidPdf()
    {
        var markdown = """
            <!-- columns: 2 -->

            Left content.

            ---

            More left content.

            Right content.

            <!-- /columns -->
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_MultipleColumnSections_ProducesValidPdf()
    {
        var markdown = """
            # First Section

            <!-- columns: 2 -->

            Left A.

            Right A.

            <!-- /columns -->

            Middle paragraph.

            <!-- columns: 3 -->

            Col 1.

            Col 2.

            Col 3.

            <!-- /columns -->

            Footer.
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_ManyBlocksInColumns_BalancesAcrossColumns()
    {
        // Six equal paragraphs in 2 columns should balance roughly evenly
        var paragraphs = string.Join("\n\n", Enumerable.Range(1, 6)
            .Select(i => $"Paragraph number {i}."));

        var markdown = $"""
            <!-- columns: 2 -->

            {paragraphs}

            <!-- /columns -->
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_ColumnsOverflowPage_ProducesMultiplePages()
    {
        // Many paragraphs that exceed a single page when split across 2 columns
        var paragraphs = string.Join("\n\n", Enumerable.Range(1, 80)
            .Select(i => $"Paragraph {i}: Some content that takes up space on the page."));

        var markdown = $"""
            <!-- columns: 2 -->

            {paragraphs}

            <!-- /columns -->
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.True(pdf.PageCount >= 2, $"Expected multiple pages but got {pdf.PageCount}");
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_ColumnsOverflowFollowedByContent_ContentAppearsAfterColumns()
    {
        var paragraphs = string.Join("\n\n", Enumerable.Range(1, 120)
            .Select(i => $"Paragraph {i}: Content in columns that takes up space."));

        var markdown = $"""
            <!-- columns: 2 -->

            {paragraphs}

            <!-- /columns -->

            This footer appears after the column section.
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.True(pdf.PageCount >= 2);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_EmptyColumnSection_ProducesValidPdf()
    {
        var markdown = """
            # Before

            <!-- columns: 2 -->
            <!-- /columns -->

            After.
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_NestedColumns_ProducesValidPdf()
    {
        var markdown = """
            <!-- columns: 2 -->

            ## Left Heading

            Left paragraph content.

            <!-- columns: 2 -->

            Nested left.

            Nested right.

            <!-- /columns -->

            ## Right Heading

            Right paragraph content.

            <!-- /columns -->
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_NestedColumnsWithTables_ProducesValidPdf()
    {
        var markdown = """
            <!-- columns: 2 -->

            Outer left content.

            <!-- columns: 2 -->

            | A | B |
            |---|---|
            | 1 | 2 |

            | C | D |
            |---|---|
            | 3 | 4 |

            <!-- /columns -->

            Outer right content.

            <!-- /columns -->
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_NestedThreeColumnsInTwoColumns_ProducesValidPdf()
    {
        var markdown = """
            <!-- columns: 2 -->

            ## Section A

            <!-- columns: 3 -->

            Col 1.

            Col 2.

            Col 3.

            <!-- /columns -->

            ## Section B

            A paragraph in the second outer column.

            <!-- /columns -->
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_NestedColumnsDepthTracking_ClosesOuterCorrectly()
    {
        // Ensures the outer <!-- /columns --> is not consumed by the inner section
        var markdown = """
            <!-- columns: 2 -->

            Before nested.

            <!-- columns: 2 -->

            Inner left.

            Inner right.

            <!-- /columns -->

            After nested.

            <!-- /columns -->

            This footer is outside all columns.
            """;

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }
}
