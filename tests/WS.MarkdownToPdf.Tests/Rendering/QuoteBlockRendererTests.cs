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
    public void Render_MultilineQuote_ProducesValidPdf()
    {
        var doc = parser.Parse("> Line one\n> Line two");

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
    public void Render_MultilineQuote_AdvancesYByTwoLines()
    {
        var markdown = "> Line one\n> Line two";
        var doc = parser.Parse(markdown);
        var quote = doc.Descendants<QuoteBlock>().First();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var initialY = context.CurrentY;

        var quoteRenderer = new QuoteBlockRenderer();
        quoteRenderer.Render(quote, context);

        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var expectedAdvance = 2 * lineHeight + LayoutConstants.ParagraphSpacing;
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
        var ex = Assert.Throws<UnsupportedMarkdownException>(() => quoteRenderer.Render(quote, context));
        Assert.True(ex.Line > 0);
        Assert.True(ex.Column > 0);
        Assert.Contains("line", ex.Message);
        Assert.Contains("column", ex.Message);
    }

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#00FF00")]
    [InlineData("#0000FF")]
    [InlineData("AABBCC")]
    public void ParseHexColor_ValidHex_RendersWithoutError(string hex)
    {
        // ParseHexColor is private, so test via rendering with a custom BarColor
        var layout = new LayoutOptions { BlockQuoteBarColor = hex };
        var doc = parser.Parse("> test");
        var quote = doc.Descendants<QuoteBlock>().First();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc, layout);
        var quoteRenderer = new QuoteBlockRenderer();

        // Should not throw — validates the color is parsed successfully
        quoteRenderer.Render(quote, context);
    }

    [Fact]
    public void ParseHexColor_InvalidHex_Throws()
    {
        var layout = new LayoutOptions { BlockQuoteBarColor = "not-a-color" };
        var doc = parser.Parse("> test");
        var quote = doc.Descendants<QuoteBlock>().First();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc, layout);
        var quoteRenderer = new QuoteBlockRenderer();

        Assert.ThrowsAny<Exception>(() => quoteRenderer.Render(quote, context));
    }
}
