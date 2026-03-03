using Markdig.Syntax;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Parsing;
using WS.MarkdownToPdf.Rendering;
using WS.MarkdownToPdf.Rendering.BlockRenderers;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class ListRendererTests
{
    private readonly PdfRenderer renderer = new();
    private readonly MarkdigParser parser = new();

    [Fact]
    public void Render_BulletedList_ProducesValidPdf()
    {
        var doc = parser.Parse("- Item 1\n- Item 2\n- Item 3");

        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_NumberedList_ProducesValidPdf()
    {
        var doc = parser.Parse("1. First\n2. Second\n3. Third");

        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_BulletedList_AdvancesYByItemCount()
    {
        var markdown = "- Item 1\n- Item 2\n- Item 3";
        var doc = parser.Parse(markdown);
        var list = doc.Descendants<ListBlock>().Single();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var initialY = context.CurrentY;

        var listRenderer = new ListRenderer();
        listRenderer.Render(list, context);

        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var expectedAdvance = 3 * lineHeight + 3 * LayoutConstants.ListItemSpacing + LayoutConstants.ParagraphSpacing;
        Assert.Equal(initialY + expectedAdvance, context.CurrentY, precision: 2);
    }

    [Fact]
    public void Render_NestedList_ThrowsUnsupportedMarkdownException()
    {
        var markdown = "- Item 1\n  - Nested item";
        var doc = parser.Parse(markdown);
        var list = doc.Descendants<ListBlock>().First();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);

        var listRenderer = new ListRenderer();
        var ex = Assert.Throws<UnsupportedMarkdownException>(() => listRenderer.Render(list, context));
        Assert.True(ex.Line > 0);
        Assert.True(ex.Column > 0);
        Assert.Contains("line", ex.Message);
        Assert.Contains("column", ex.Message);
    }
}
