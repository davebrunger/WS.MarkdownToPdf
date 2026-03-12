using Markdig.Extensions.Tables;
using Markdig.Syntax;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering.BlockRenderers;

/// <summary>
/// Renders a <see cref="Table"/> centred on the page, sized to content, with a horizontal
/// rule between the header and body rows.  The first column is right-aligned; all subsequent
/// columns are left-aligned.  Header text is bold.
///
/// When the table does not fit, column widths are reduced progressively:
/// 1. Try unwrapped natural widths.
/// 2. Wrap headers that are wider than their data onto 2 lines, then 3, etc.
/// 3. Wrap the widest data column onto 2 lines, then the next widest, etc.
///    Repeat at 3 lines, 4 lines, etc.
/// 4. Throw if the table still cannot fit.
/// </summary>
public class TableRenderer
{
    private readonly InlineRenderer inlineRenderer = new();

    public void Render(Table table, RenderContext context)
    {
        var rows = table.OfType<TableRow>().ToList();
        if (rows.Count == 0) return;

        var columnCount = rows.Max(r => r.Count);
        var lineHeight = context.Layout.BodyFontSize * context.Layout.LineSpacingMultiplier;

        var columnWidths = FitColumnWidths(rows, columnCount, table.Line + 1, context);
        var tableWidth = columnWidths.Sum();

        var rowHeights = ComputeRowHeights(rows, columnCount, columnWidths, lineHeight, context);
        var totalHeight = rowHeights.Sum() + context.Layout.ParagraphSpacing;
        context.EnsureSpace(totalHeight);

        var tableLeft = context.ContentLeft + (context.ContentWidth - tableWidth) / 2;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var cellX = tableLeft;

            for (var col = 0; col < columnCount; col++)
            {
                if (col < row.Count)
                {
                    var cellRuns = GetCellRuns(row, col, context);
                    if (cellRuns.Count > 0)
                    {
                        var clipRect = new XRect(cellX, context.CurrentY, columnWidths[col], rowHeights[rowIndex]);
                        var state = context.Graphics.Save();
                        context.Graphics.IntersectClip(clipRect);

                        RenderWrappedCell(cellRuns, col, columnCount, row.IsHeader, cellX, columnWidths[col], rowHeights[rowIndex], lineHeight, context);

                        context.Graphics.Restore(state);
                    }
                }

                cellX += columnWidths[col];
            }

            if (row.IsHeader)
            {
                var ruleY = context.CurrentY + rowHeights[rowIndex];
                var pen = new XPen(XColors.Black, context.Layout.TableHeaderRuleThickness);
                context.Graphics.DrawLine(pen, tableLeft, ruleY, tableLeft + tableWidth, ruleY);
            }

            context.CurrentY += rowHeights[rowIndex];
        }

        context.CurrentY += context.Layout.ParagraphSpacing;
    }

    public double MeasureHeight(Table table, RenderContext context)
    {
        var rows = table.OfType<TableRow>().ToList();
        if (rows.Count == 0) return context.Layout.ParagraphSpacing;

        var columnCount = rows.Max(r => r.Count);
        var lineHeight = context.Layout.BodyFontSize * context.Layout.LineSpacingMultiplier;

        var columnWidths = FitColumnWidths(rows, columnCount, table.Line + 1, context);
        var rowHeights = ComputeRowHeights(rows, columnCount, columnWidths, lineHeight, context);
        return rowHeights.Sum() + context.Layout.ParagraphSpacing;
    }

    // ── Column-width fitting algorithm ──────────────────────────────────

    /// <summary>
    /// Determines column widths using a progressive wrapping strategy:
    /// <list type="number">
    ///   <item>Use unwrapped natural widths (max of header and data). If fits, done.</item>
    ///   <item>Progressively wrap headers that are wider than their data column
    ///         onto 2 lines, 3 lines, etc. until they all fit or can't shrink further.</item>
    ///   <item>Progressively wrap data columns (widest first) onto 2 lines,
    ///         then 3 lines, etc.</item>
    ///   <item>Throw if the table still cannot fit.</item>
    /// </list>
    /// </summary>
    private double[] FitColumnWidths(
        List<TableRow> rows, int columnCount, int tableLine, RenderContext context)
    {
        var available = context.ContentWidth;
        var padding = 2 * context.Layout.TableCellPaddingH;

        // Gather per-column info: header runs, data runs, natural widths
        var headerRunsByCol = new List<TextRun>[columnCount];
        var dataWidths = new double[columnCount];
        var headerWidths = new double[columnCount];

        for (var col = 0; col < columnCount; col++)
            headerRunsByCol[col] = [];

        foreach (var row in rows)
        {
            for (var col = 0; col < columnCount && col < row.Count; col++)
            {
                var cellRuns = GetCellRuns(row, col, context);
                var textWidth = cellRuns.Sum(r => context.Graphics.MeasureString(r.Text, r.Font).Width);
                var cellWidth = textWidth + padding;

                if (row.IsHeader)
                {
                    headerRunsByCol[col] = cellRuns;
                    headerWidths[col] = Math.Max(headerWidths[col], cellWidth);
                }
                else
                {
                    dataWidths[col] = Math.Max(dataWidths[col], cellWidth);
                }
            }
        }

        // Collect all data-row runs per column for step 3
        var dataRunsByCol = new List<List<TextRun>>[columnCount];
        for (var col = 0; col < columnCount; col++)
            dataRunsByCol[col] = [];

        foreach (var row in rows)
        {
            if (row.IsHeader) continue;
            for (var col = 0; col < columnCount && col < row.Count; col++)
            {
                var cellRuns = GetCellRuns(row, col, context);
                if (cellRuns.Count > 0)
                    dataRunsByCol[col].Add(cellRuns);
            }
        }

        // Step 1: natural widths (max of header and data per column)
        var widths = new double[columnCount];
        for (var c = 0; c < columnCount; c++)
            widths[c] = Math.Max(headerWidths[c], dataWidths[c]);

        if (widths.Sum() <= available)
            return widths;

        // Step 2: progressively wrap headers that are wider than their data
        for (var targetLines = 2; targetLines <= 20; targetLines++)
        {
            var anyHeaderWider = false;
            for (var c = 0; c < columnCount; c++)
            {
                if (headerWidths[c] > dataWidths[c] && headerRunsByCol[c].Count > 0)
                {
                    anyHeaderWider = true;
                    var wrappedHeaderWidth = FindWidthForLineCount(
                        headerRunsByCol[c], targetLines, context.Graphics, padding);
                    headerWidths[c] = wrappedHeaderWidth;
                    widths[c] = Math.Max(headerWidths[c], dataWidths[c]);
                }
            }

            if (!anyHeaderWider)
                break;

            if (widths.Sum() <= available)
                return widths;
        }

        // Step 3: progressively wrap data columns, widest first
        for (var targetLines = 2; targetLines <= 20; targetLines++)
        {
            // Sort column indices by current width descending
            var colsByWidth = Enumerable.Range(0, columnCount)
                .OrderByDescending(c => widths[c])
                .ToList();

            foreach (var c in colsByWidth)
            {
                if (dataRunsByCol[c].Count == 0) continue;

                // Find the width that wraps every data cell to at most targetLines
                var newDataWidth = 0.0;
                foreach (var cellRuns in dataRunsByCol[c])
                {
                    var w = FindWidthForLineCount(cellRuns, targetLines, context.Graphics, padding);
                    newDataWidth = Math.Max(newDataWidth, w);
                }

                dataWidths[c] = newDataWidth;

                // Also re-wrap header if needed to fit within the new data width
                if (headerRunsByCol[c].Count > 0)
                {
                    var headerMin = FindMinWidth(headerRunsByCol[c], context.Graphics, padding);
                    headerWidths[c] = Math.Min(headerWidths[c], Math.Max(newDataWidth, headerMin));
                }

                widths[c] = Math.Max(headerWidths[c], dataWidths[c]);

                if (widths.Sum() <= available)
                    return widths;
            }
        }

        // Step 4: cannot fit
        throw new UnsupportedMarkdownException(
            $"Table (too wide to fit in {available:F0}pt)", tableLine, 1);
    }

    /// <summary>
    /// Binary-searches for the minimum column width (including padding) that causes
    /// the given text runs to wrap into at most <paramref name="targetLines"/> lines.
    /// </summary>
    private static double FindWidthForLineCount(
        List<TextRun> runs, int targetLines, XGraphics graphics, double padding)
    {
        var maxContentWidth = runs.Sum(r => graphics.MeasureString(r.Text, r.Font).Width);
        var minContentWidth = FindMinContentWidth(runs, graphics);

        var lo = minContentWidth;
        var hi = maxContentWidth;

        // Quick check: does the content already fit in targetLines at max width?
        if (LineWrapper.WrapLinesPreferCommaBreak(runs, graphics, hi).Count <= targetLines)
        {
            // Binary search for the smallest width that still produces <= targetLines
            for (var i = 0; i < 30; i++)
            {
                var mid = (lo + hi) / 2;
                if (LineWrapper.WrapLinesPreferCommaBreak(runs, graphics, mid).Count <= targetLines)
                    hi = mid;
                else
                    lo = mid;
            }

            return hi + padding;
        }

        // Can't fit in targetLines even at full width — return min word width
        return minContentWidth + padding;
    }

    /// <summary>
    /// Returns the minimum column width (including padding) — the widest single word.
    /// </summary>
    private static double FindMinWidth(
        List<TextRun> runs, XGraphics graphics, double padding)
    {
        return FindMinContentWidth(runs, graphics) + padding;
    }

    /// <summary>
    /// Returns the widest single word across all runs (content width, no padding).
    /// </summary>
    private static double FindMinContentWidth(List<TextRun> runs, XGraphics graphics)
    {
        var minWidth = 0.0;
        foreach (var run in runs)
        {
            var segments = LineWrapper.SplitIntoWordSegments(run.Text);
            foreach (var segment in segments)
            {
                var w = graphics.MeasureString(segment.Trim(), run.Font).Width;
                minWidth = Math.Max(minWidth, w);
            }
        }

        return minWidth;
    }

    // ── Cell rendering ──────────────────────────────────────────────────

    private void RenderWrappedCell(
        List<TextRun> cellRuns, int col, int columnCount, bool isHeader,
        double cellX, double colWidth, double rowHeight, double lineHeight,
        RenderContext context)
    {
        var cellContentWidth = colWidth - (2 * context.Layout.TableCellPaddingH);
        var wrappedLines = LineWrapper.WrapLinesPreferCommaBreak(cellRuns, context.Graphics, cellContentWidth);

        var textBlockHeight = wrappedLines.Count * lineHeight;

        // Vertical alignment: headers bottom-aligned, data middle-aligned
        double lineY;
        if (isHeader)
            lineY = context.CurrentY + rowHeight - textBlockHeight - context.Layout.TableCellPaddingV;
        else
            lineY = context.CurrentY + (rowHeight - textBlockHeight) / 2;

        foreach (var line in wrappedLines)
        {
            var lineWidth = line.Sum(r => context.Graphics.MeasureString(r.Text, r.Font).Width);
            var textX = ComputeHorizontalX(col, columnCount, cellX, colWidth, lineWidth, context);

            foreach (var run in line)
            {
                context.Graphics.DrawString(
                    run.Text, run.Font, XBrushes.Black,
                    new XPoint(textX, lineY + context.Layout.BodyFontSize));
                textX += context.Graphics.MeasureString(run.Text, run.Font).Width;
            }

            lineY += lineHeight;
        }
    }

    /// <summary>
    /// First column: right-aligned. Last column: left-aligned. All others: centre-aligned.
    /// </summary>
    private static double ComputeHorizontalX(
        int col, int columnCount, double cellX, double colWidth,
        double lineWidth, RenderContext context)
    {
        var padH = context.Layout.TableCellPaddingH;
        var leftEdge = cellX + padH;
        var rightEdge = cellX + colWidth - padH;

        if (col == 0)
        {
            // Right-aligned
            return Math.Max(leftEdge, rightEdge - lineWidth);
        }

        if (col == columnCount - 1)
        {
            // Left-aligned
            return leftEdge;
        }

        // Centre-aligned
        var centreStart = cellX + (colWidth - lineWidth) / 2;
        return Math.Max(leftEdge, centreStart);
    }

    private List<TextRun> GetCellRuns(TableRow row, int col, RenderContext context)
    {
        if (col >= row.Count) return [];

        var cell = row[col] as TableCell;
        if (cell is null) return [];

        var paragraph = cell.OfType<ParagraphBlock>().FirstOrDefault();
        if (paragraph?.Inline is null) return [];

        var runs = inlineRenderer.GetTextRuns(paragraph.Inline, context.Layout.BodyFontSize, context.Layout);

        if (row.IsHeader)
        {
            runs = runs.Select(r =>
                r with { Font = new XFont(r.Font.FontFamily.Name, r.Font.Size, r.Font.Style | XFontStyleEx.Bold) })
                .ToList();
        }

        return runs;
    }

    private double[] ComputeRowHeights(
        List<TableRow> rows, int columnCount, double[] columnWidths,
        double lineHeight, RenderContext context)
    {
        var rowHeights = new double[rows.Count];

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var maxLines = 1;

            for (var col = 0; col < columnCount && col < row.Count; col++)
            {
                var cellRuns = GetCellRuns(row, col, context);
                if (cellRuns.Count == 0) continue;

                var cellContentWidth = columnWidths[col] - (2 * context.Layout.TableCellPaddingH);
                    var wrappedLines = LineWrapper.WrapLinesPreferCommaBreak(cellRuns, context.Graphics, cellContentWidth);
                maxLines = Math.Max(maxLines, wrappedLines.Count);
            }

            rowHeights[rowIndex] = maxLines * lineHeight + (2 * context.Layout.TableCellPaddingV);
        }

        return rowHeights;
    }
}
