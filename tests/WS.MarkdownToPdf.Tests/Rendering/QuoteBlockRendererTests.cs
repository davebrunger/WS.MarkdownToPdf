using Markdig.Syntax;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Parsing;
using WS.MarkdownToPdf.Rendering;
using WS.MarkdownToPdf.Rendering.BlockRenderers;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class QuoteBlockRendererTests
{
    private readonly PdfRenderer renderer = new();
    private readonly MarkdigParser parser = new();

    [Fact]
    public void Render_SingleQuote_ProducesValidPdf()
    {
        var doc = parser.Parse("> This is a quote");

        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_SingleQuote_AdvancesYPosition()
    {
        var markdown = "> This is a quote";
        var doc = parser.Parse(markdown);
        var quote = doc.Descendants<QuoteBlock>().Single();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var initialY = context.CurrentY;

        var quoteRenderer = new QuoteBlockRenderer();
        quoteRenderer.Render(quote, context);

        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var expectedAdvance = lineHeight + LayoutConstants.ParagraphSpacing;
        Assert.Equal(initialY + expectedAdvance, context.CurrentY, precision: 2);
    }

    [Fact]
    public void Render_NestedQuote_ThrowsUnsupportedMarkdownException()
    {
        var markdown = "> Outer quote\n>> Nested quote";
        var doc = parser.Parse(markdown);
        var quote = doc.Descendants<QuoteBlock>().First();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);

        var quoteRenderer = new QuoteBlockRenderer();
        Assert.Throws<UnsupportedMarkdownException>(() => quoteRenderer.Render(quote, context));
    }
}
