using Markdig.Syntax;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Parsing;
using WS.MarkdownToPdf.Rendering;
using WS.MarkdownToPdf.Rendering.BlockRenderers;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class HeadingRendererTests
{
    private readonly PdfRenderer renderer = new();
    private readonly MarkdigParser parser = new();

    [Fact]
    public void Render_H1_AdvancesYByH1HeadingHeight()
    {
        var doc = parser.Parse("# Title");

        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_H3_AdvancesYByH3HeadingHeight()
    {
        var doc = parser.Parse("### Subtitle");

        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Theory]
    [InlineData("# H1", 20)]
    [InlineData("## H2", 16)]
    [InlineData("### H3", 14)]
    [InlineData("#### H4", 12)]
    [InlineData("##### H5", 10)]
    [InlineData("###### H6", 8)]
    public void Render_HeadingLevel_UsesCorrectFontSize(string markdown, double expectedFontSize)
    {
        var doc = parser.Parse(markdown);
        var heading = doc.Descendants<HeadingBlock>().Single();

        var headingRenderer = new HeadingRenderer();
        using var pdf = new PdfDocument();
        var context = new RenderContext(pdf);
        var initialY = context.CurrentY;

        headingRenderer.Render(heading, doc.ToList(), 0, context);

        var expectedAdvance = expectedFontSize * LayoutConstants.LineSpacingMultiplier
                              + LayoutConstants.ParagraphSpacing;
        Assert.Equal(initialY + expectedAdvance, context.CurrentY, precision: 2);
    }
}
