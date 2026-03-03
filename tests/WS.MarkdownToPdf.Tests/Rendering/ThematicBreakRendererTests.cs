using Markdig.Syntax;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Parsing;
using WS.MarkdownToPdf.Rendering;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class ThematicBreakRendererTests
{
    private readonly PdfRenderer renderer = new();
    private readonly MarkdigParser parser = new();

    [Fact]
    public void Render_HorizontalRule_ProducesValidPdf()
    {
        var doc = parser.Parse("---");

        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_HorizontalRule_AdvancesYPosition()
    {
        var doc = parser.Parse("---");

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var initialY = context.CurrentY;

        var thematicBreak = doc.Descendants<Markdig.Syntax.ThematicBreakBlock>().Single();
        var thematicBreakRenderer = new WS.MarkdownToPdf.Rendering.BlockRenderers.ThematicBreakRenderer();
        thematicBreakRenderer.Render(thematicBreak, context);

        var expectedAdvance = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier
                              + LayoutConstants.ParagraphSpacing;
        Assert.Equal(initialY + expectedAdvance, context.CurrentY, precision: 2);
    }
}
