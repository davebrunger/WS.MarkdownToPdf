using Markdig.Syntax;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Rendering;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class PdfRendererTests
{
    private readonly PdfRenderer sut = new();

    [Fact]
    public void Render_EmptyDocument_ProducesValidSinglePagePdf()
    {
        var document = new MarkdownDocument();

        using var pdf = sut.Render(document);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_UnsupportedNodeType_ThrowsUnsupportedMarkdownException()
    {
        var document = new MarkdownDocument();
        document.Add(new FencedCodeBlock(null!));

        Assert.Throws<UnsupportedMarkdownException>(() => sut.Render(document));
    }
}
