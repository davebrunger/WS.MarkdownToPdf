using Markdig.Extensions.Tables;
using Markdig.Syntax;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering.BlockRenderers;

/// <summary>
/// Renders a <see cref="Table"/> as a grid with header row in bold.
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
        var columnWidth = context.ContentWidth / columnCount;
        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var rowHeight = lineHeight + (2 * LayoutConstants.TableCellPadding);
        var pen = new XPen(XColors.Black, 0.5);

        var startY = context.CurrentY;

        foreach (var row in rows)
        {
            var cellX = context.ContentLeft;

            // Draw row cells
            for (var col = 0; col < columnCount; col++)
            {
                // Draw cell border
                context.Graphics.DrawRectangle(pen, cellX, context.CurrentY, columnWidth, rowHeight);

                if (col < row.Count)
                {
                    var cell = row[col] as TableCell;
                    if (cell is not null)
                    {
                        var paragraph = cell.OfType<ParagraphBlock>().FirstOrDefault();
                        if (paragraph?.Inline is not null)
                        {
                            var isHeader = row.IsHeader;
                            var fontStyle = isHeader ? XFontStyleEx.Bold : XFontStyleEx.Regular;
                            var runs = inlineRenderer.GetTextRuns(paragraph.Inline, LayoutConstants.BodyFontSize);

                            var textX = cellX + LayoutConstants.TableCellPadding;
                            var textY = context.CurrentY + LayoutConstants.TableCellPadding + LayoutConstants.BodyFontSize;

                            foreach (var run in runs)
                            {
                                var font = isHeader
                                    ? new XFont(run.Font.FontFamily.Name, run.Font.Size, run.Font.Style | XFontStyleEx.Bold)
                                    : run.Font;

                                context.Graphics.DrawString(
                                    run.Text,
                                    font,
                                    XBrushes.Black,
                                    new XPoint(textX, textY));

                                textX += context.Graphics.MeasureString(run.Text, font).Width;
                            }
                        }
                    }
                }

                cellX += columnWidth;
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
}
