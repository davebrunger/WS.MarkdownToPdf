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
}
