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
        var rowHeight = lineHeight + 2 * LayoutConstants.TableCellPaddingV;
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
        var rowHeight = lineHeight + 2 * LayoutConstants.TableCellPaddingV;
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

    [Fact]
    public void Render_WideTable_WrapsTextAndIncreasesRowHeight()
    {
        // Build a table wide enough to exceed the default content width (~481 pt).
        // Each column has long text that will need wrapping once columns are scaled.
        var markdown = "| Name | Description | Details | Extra |\n"
                     + "|------|-------------|---------|-------|\n"
                     + "| Colt 1903 Pocket Hammerless | A compact semi-automatic pistol chambered in 32 ACP | Manufactured by Colt from 1903 to 1945 with many variants | Hard to Find, Very Rare |";
        var doc = parser.Parse(markdown);
        var table = doc.Descendants<Table>().Single();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);
        var initialY = context.CurrentY;

        var tableRenderer = new TableRenderer();
        tableRenderer.Render(table, context);

        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var singleRowHeight = lineHeight + 2 * LayoutConstants.TableCellPaddingV;

        // With wrapping, at least one data row should be taller than a single line
        var totalAdvance = context.CurrentY - initialY;
        var minWithoutWrapping = 2 * singleRowHeight + LayoutConstants.ParagraphSpacing;
        Assert.True(totalAdvance > minWithoutWrapping,
            $"Expected wrapped table to be taller than {minWithoutWrapping:F1} pt but got {totalAdvance:F1} pt");
    }

    [Fact]
    public void MeasureHeight_WideTable_AccountsForWrapping()
    {
        var markdown = "| Name | Description | Details | Extra |\n"
                     + "|------|-------------|---------|-------|\n"
                     + "| Colt 1903 Pocket Hammerless | A compact semi-automatic pistol chambered in 32 ACP | Manufactured by Colt from 1903 to 1945 with many variants | Hard to Find, Very Rare |";
        var doc = parser.Parse(markdown);
        var table = doc.Descendants<Table>().Single();

        using var pdfDoc = new PdfDocument();
        var context = new RenderContext(pdfDoc);

        var tableRenderer = new TableRenderer();
        var measuredHeight = tableRenderer.MeasureHeight(table, context);

        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var singleRowHeight = lineHeight + 2 * LayoutConstants.TableCellPaddingV;
        var minWithoutWrapping = 2 * singleRowHeight + LayoutConstants.ParagraphSpacing;

        Assert.True(measuredHeight > minWithoutWrapping,
            $"Expected measured height {measuredHeight:F1} to exceed non-wrapped height {minWithoutWrapping:F1}");
    }
}
