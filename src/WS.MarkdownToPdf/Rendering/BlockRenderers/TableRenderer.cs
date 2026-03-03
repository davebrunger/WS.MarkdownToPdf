using Markdig.Extensions.Tables;
using Markdig.Syntax;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering.BlockRenderers;

/// <summary>
/// Renders a <see cref="Table"/> centred on the page, sized to content, with a horizontal
/// rule between the header and body rows.  The first column is right-aligned; all subsequent
/// columns are left-aligned.  Header text is bold.
/// </summary>
public class TableRenderer
{
    private readonly InlineRenderer inlineRenderer = new();

    public void Render(Table table, RenderContext context)
    {
        var height = MeasureHeight(table, context);
        context.EnsureSpace(height);

        var rows = table.OfType<TableRow>().ToList();
        if (rows.Count == 0) return;

        var columnCount = rows.Max(r => r.Count);
        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var rowHeight = lineHeight + (2 * LayoutConstants.TableCellPadding);

        // Measure natural column widths (widest cell content + padding on each side)
        var columnWidths = MeasureColumnWidths(rows, columnCount, context);

        var tableWidth = columnWidths.Sum();

        // Clamp table to available content width by scaling columns proportionally
        if (tableWidth > context.ContentWidth)
        {
            var scale = context.ContentWidth / tableWidth;
            for (var c = 0; c < columnWidths.Length; c++)
                columnWidths[c] *= scale;
            tableWidth = context.ContentWidth;
        }

        var tableLeft = context.ContentLeft + (context.ContentWidth - tableWidth) / 2;

        foreach (var row in rows)
        {
            var cellX = tableLeft;

            for (var col = 0; col < columnCount; col++)
            {
                if (col < row.Count)
                {
                    var cell = row[col] as TableCell;
                    if (cell is not null)
                    {
                        var paragraph = cell.OfType<ParagraphBlock>().FirstOrDefault();
                        if (paragraph?.Inline is not null)
                        {
                            var isHeader = row.IsHeader;
                            var runs = inlineRenderer.GetTextRuns(paragraph.Inline, LayoutConstants.BodyFontSize);

                            // Measure total text width for alignment
                            var totalTextWidth = 0.0;
                            var fonts = new List<(string Text, XFont Font)>();
                            foreach (var run in runs)
                            {
                                var font = isHeader
                                    ? new XFont(run.Font.FontFamily.Name, run.Font.Size, run.Font.Style | XFontStyleEx.Bold)
                                    : run.Font;
                                var w = context.Graphics.MeasureString(run.Text, font).Width;
                                totalTextWidth += w;
                                fonts.Add((run.Text, font));
                            }

                            // First column (col 0): right-aligned; others: left-aligned
                            double textX;
                            if (col == 0)
                            {
                                textX = cellX + columnWidths[col] - LayoutConstants.TableCellPadding - totalTextWidth;
                            }
                            else
                            {
                                textX = cellX + LayoutConstants.TableCellPadding;
                            }

                            var textY = context.CurrentY + LayoutConstants.TableCellPadding + LayoutConstants.BodyFontSize;

                            foreach (var (text, font) in fonts)
                            {
                                context.Graphics.DrawString(
                                    text,
                                    font,
                                    XBrushes.Black,
                                    new XPoint(textX, textY));

                                textX += context.Graphics.MeasureString(text, font).Width;
                            }
                        }
                    }
                }

                cellX += columnWidths[col];
            }

            // Draw horizontal rule under header row
            if (row.IsHeader)
            {
                var ruleY = context.CurrentY + rowHeight;
                var pen = new XPen(XColors.Black, 0.5);
                context.Graphics.DrawLine(pen, tableLeft, ruleY, tableLeft + tableWidth, ruleY);
            }

            context.CurrentY += rowHeight;
        }

        context.CurrentY += LayoutConstants.ParagraphSpacing;
    }

    public double MeasureHeight(Table table, RenderContext context)
    {
        var rowCount = table.OfType<TableRow>().Count();
        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var rowHeight = lineHeight + (2 * LayoutConstants.TableCellPadding);
        return rowCount * rowHeight + LayoutConstants.ParagraphSpacing;
    }

    /// <summary>
    /// Measures the natural width of each column (widest cell content + cell padding on both sides).
    /// Uses bold font for header cells to get accurate measurements.
    /// </summary>
    private double[] MeasureColumnWidths(List<TableRow> rows, int columnCount, RenderContext context)
    {
        var widths = new double[columnCount];

        foreach (var row in rows)
        {
            for (var col = 0; col < columnCount && col < row.Count; col++)
            {
                var cell = row[col] as TableCell;
                if (cell is null) continue;

                var paragraph = cell.OfType<ParagraphBlock>().FirstOrDefault();
                if (paragraph?.Inline is null) continue;

                var runs = inlineRenderer.GetTextRuns(paragraph.Inline, LayoutConstants.BodyFontSize);
                var textWidth = 0.0;

                foreach (var run in runs)
                {
                    var font = row.IsHeader
                        ? new XFont(run.Font.FontFamily.Name, run.Font.Size, run.Font.Style | XFontStyleEx.Bold)
                        : run.Font;
                    textWidth += context.Graphics.MeasureString(run.Text, font).Width;
                }

                var cellWidth = textWidth + (2 * LayoutConstants.TableCellPadding);
                widths[col] = Math.Max(widths[col], cellWidth);
            }
        }

        return widths;
    }
}
