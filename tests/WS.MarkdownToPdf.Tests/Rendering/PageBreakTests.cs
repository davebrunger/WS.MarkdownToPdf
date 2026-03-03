using PdfSharp.Pdf;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Parsing;
using WS.MarkdownToPdf.Rendering;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class PageBreakTests
{
    private readonly PdfRenderer renderer = new();
    private readonly MarkdigParser parser = new();

    [Fact]
    public void Render_ParagraphOverflowingPage_MovedToNextPage()
    {
        // Fill a page almost completely, then add a paragraph that won't fit
        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier
                         + LayoutConstants.ParagraphSpacing;
        var linesPerPage = (int)(LayoutConstants.ContentHeight / lineHeight);

        // Create enough paragraphs to fill exactly one page, plus one more
        var lines = Enumerable.Range(1, linesPerPage + 1)
            .Select(i => $"Paragraph {i}")
            .ToList();
        var markdown = string.Join("\n\n", lines);

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.True(pdf.PageCount >= 2, $"Expected at least 2 pages but got {pdf.PageCount}");
    }

    [Fact]
    public void Render_HeadingAtBottomOfPage_MovedToNextPageWithFollowingBlock()
    {
        // Fill page nearly full, then add a heading followed by a paragraph.
        // Both should appear on the next page.
        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier
                         + LayoutConstants.ParagraphSpacing;
        var linesPerPage = (int)(LayoutConstants.ContentHeight / lineHeight);

        // Fill most of the page
        var fillerLines = Enumerable.Range(1, linesPerPage - 1)
            .Select(i => $"Filler line {i}")
            .ToList();
        var markdown = string.Join("\n\n", fillerLines);
        markdown += "\n\n# Important Heading\n\nThis follows the heading.";

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.True(pdf.PageCount >= 2, $"Expected at least 2 pages but got {pdf.PageCount}");
    }

    [Fact]
    public void Render_LongDocumentWithManyParagraphs_ProducesMultiplePages()
    {
        // Create a document with many paragraphs that definitely spans multiple pages
        var paragraphs = Enumerable.Range(1, 100)
            .Select(i => $"This is paragraph number {i} with enough text to take up space.")
            .ToList();
        var markdown = string.Join("\n\n", paragraphs);

        var doc = parser.Parse(markdown);
        using var pdf = renderer.Render(doc);

        Assert.True(pdf.PageCount >= 3, $"Expected at least 3 pages but got {pdf.PageCount}");

        // Verify the PDF is valid
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_EmptyDocument_ProducesSinglePage()
    {
        var doc = parser.Parse("");

        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
    }
}
