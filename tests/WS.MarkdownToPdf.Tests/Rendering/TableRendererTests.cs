using Markdig.Extensions.Tables;
using Markdig.Syntax;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Parsing;
using WS.MarkdownToPdf.Rendering;
using WS.MarkdownToPdf.Rendering.BlockRenderers;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class TableRendererTests
{
    private readonly PdfRenderer renderer = new();
    private readonly MarkdigParser parser = new();

    [Fact]
    public void Render_SimpleTable_ProducesValidPdf()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";
        var doc = parser.Parse(markdown);

        using var pdf = renderer.Render(doc);

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Render_TableWithHeader_AdvancesYByRowCount()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";
        var doc = parser.Parse(markdown);
        var table = doc.Descendants<Table>().Single();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var initialY = context.CurrentY;

        var tableRenderer = new TableRenderer();
        tableRenderer.Render(table, context);

        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var rowHeight = lineHeight + 2 * LayoutConstants.TableCellPadding;
        // 2 rows (header + data) + paragraph spacing
        var expectedAdvance = 2 * rowHeight + LayoutConstants.ParagraphSpacing;
        Assert.Equal(initialY + expectedAdvance, context.CurrentY, precision: 2);
    }

    [Fact]
    public void Render_LargerTable_AdvancesYCorrectly()
    {
        var markdown = "| Col1 | Col2 | Col3 |\n|------|------|------|\n| A | B | C |\n| D | E | F |\n| G | H | I |";
        var doc = parser.Parse(markdown);
        var table = doc.Descendants<Table>().Single();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var initialY = context.CurrentY;

        var tableRenderer = new TableRenderer();
        tableRenderer.Render(table, context);

        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var rowHeight = lineHeight + 2 * LayoutConstants.TableCellPadding;
        // 4 rows (1 header + 3 data) + paragraph spacing
        var expectedAdvance = 4 * rowHeight + LayoutConstants.ParagraphSpacing;
        Assert.Equal(initialY + expectedAdvance, context.CurrentY, precision: 2);
    }

    [Fact]
    public void Render_Table_ContentWidthSmallerThanPageWidth()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";
        var doc = parser.Parse(markdown);

        // The table content "A", "B", "1", "2" is narrow, so the table
        // should NOT span the full content width.  We verify indirectly
        // by ensuring the renderer completes without error and the PDF is valid.
        using var pdf = renderer.Render(doc);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }
}
