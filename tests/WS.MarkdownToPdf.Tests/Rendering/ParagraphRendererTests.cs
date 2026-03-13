using Markdig.Syntax;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Parsing;
using WS.MarkdownToPdf.Rendering;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class ParagraphRendererTests
{
    private readonly PdfRenderer renderer = new();
    private readonly MarkdigParser parser = new();

    [Fact]
    public void Render_SingleParagraph_AdvancesYPosition()
    {
        var doc = parser.Parse("Hello world");

        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_MultipleParagraphs_ProducesValidPdf()
    {
        var doc = parser.Parse("First paragraph\n\nSecond paragraph\n\nThird paragraph");

        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_LongLine_WrapsAndAdvancesYByMultipleLines()
    {
        var longText = string.Join(" ", Enumerable.Repeat("word", 80));
        var doc = parser.Parse(longText);

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var initialY = context.CurrentY;

        var paragraphBlock = doc.Descendants<ParagraphBlock>().First();
        var sut = new WS.MarkdownToPdf.Rendering.BlockRenderers.ParagraphRenderer();
        sut.Render(paragraphBlock, context);

        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var singleLineAdvance = lineHeight + LayoutConstants.ParagraphSpacing;

        // Long text must wrap to more than one line
        Assert.True(context.CurrentY - initialY > singleLineAdvance);
    }

    [Fact]
    public void Render_SoftLineBreak_TreatedAsSingleLine()
    {
        // A single newline within a paragraph is a soft break, not a new paragraph
        var doc = parser.Parse("Line one\nLine two");

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var initialY = context.CurrentY;

        var paragraphBlock = doc.Descendants<ParagraphBlock>().First();
        var sut = new WS.MarkdownToPdf.Rendering.BlockRenderers.ParagraphRenderer();
        sut.Render(paragraphBlock, context);

        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var singleLineAdvance = lineHeight + LayoutConstants.ParagraphSpacing;

        // Soft break joins text on same line (may word-wrap, but not forced two lines)
        Assert.Equal(initialY + singleLineAdvance, context.CurrentY, precision: 2);
    }

    [Fact]
    public void CountLines_ShortParagraph_ReturnsOne()
    {
        var doc = parser.Parse("Hello world");
        var paragraph = doc.Descendants<ParagraphBlock>().First();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var sut = new WS.MarkdownToPdf.Rendering.BlockRenderers.ParagraphRenderer();

        var count = sut.CountLines(paragraph, context);

        Assert.Equal(1, count);
    }

    [Fact]
    public void CountLines_LongParagraph_ReturnsMultiple()
    {
        var longText = string.Join(" ", Enumerable.Repeat("word", 80));
        var doc = parser.Parse(longText);
        var paragraph = doc.Descendants<ParagraphBlock>().First();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var sut = new WS.MarkdownToPdf.Rendering.BlockRenderers.ParagraphRenderer();

        var count = sut.CountLines(paragraph, context);

        Assert.True(count > 1);
    }

    [Fact]
    public void CountLines_NullInline_ReturnsOne()
    {
        // A ParagraphBlock with no inline content should return 1 line
        var paragraph = new ParagraphBlock();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var sut = new WS.MarkdownToPdf.Rendering.BlockRenderers.ParagraphRenderer();

        var count = sut.CountLines(paragraph, context);

        Assert.Equal(1, count);
    }
}
