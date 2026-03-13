using PdfSharp.Drawing;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class JustifiedLineDrawerTests
{
    private readonly XGraphics graphics;
    private readonly XFont font;
    private readonly LayoutOptions layout = new();

    public JustifiedLineDrawerTests()
    {
        FontSetup.EnsureInitialized();
        var doc = new PdfDocument();
        var page = doc.AddPage();
        graphics = XGraphics.FromPdfPage(page);
        font = new XFont(layout.BodyFontFamily, layout.BodyFontSize, XFontStyleEx.Regular);
    }

    [Fact]
    public void DrawLine_EmptyRuns_DoesNotThrow()
    {
        JustifiedLineDrawer.DrawLine([], graphics, 50, 50, 200, layout, justify: true);
    }

    [Fact]
    public void DrawLine_SingleWord_DoesNotThrow()
    {
        var runs = new List<TextRun> { new("Hello", font) };

        JustifiedLineDrawer.DrawLine(runs, graphics, 50, 50, 200, layout, justify: true);
    }

    [Fact]
    public void DrawLine_MultipleWords_Justified_DoesNotThrow()
    {
        var runs = new List<TextRun> { new("Hello world test", font) };

        JustifiedLineDrawer.DrawLine(runs, graphics, 50, 50, 200, layout, justify: true);
    }

    [Fact]
    public void DrawLine_NotJustified_DoesNotThrow()
    {
        var runs = new List<TextRun> { new("Hello world test", font) };

        JustifiedLineDrawer.DrawLine(runs, graphics, 50, 50, 200, layout, justify: false);
    }

    [Fact]
    public void DrawLine_WithStrikethrough_DoesNotThrow()
    {
        var runs = new List<TextRun> { new("strikethrough text", font, IsStrikethrough: true) };

        JustifiedLineDrawer.DrawLine(runs, graphics, 50, 50, 200, layout, justify: true);
    }
}
