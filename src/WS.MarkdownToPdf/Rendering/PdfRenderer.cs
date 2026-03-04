using System.Text.RegularExpressions;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.BlockRenderers;

namespace WS.MarkdownToPdf.Rendering;

/// <summary>
/// Walks the Markdig AST and dispatches each block to the appropriate renderer.
/// Recognises HTML-comment directives for multi-column layout with automatic
/// content distribution and page-overflow support.
/// </summary>
public partial class PdfRenderer : IPdfRenderer
{
    private readonly HeadingRenderer headingRenderer = new();
    private readonly ParagraphRenderer paragraphRenderer = new();
    private readonly ThematicBreakRenderer thematicBreakRenderer = new();
    private readonly ListRenderer listRenderer = new();
    private readonly QuoteBlockRenderer quoteBlockRenderer = new();
    private readonly TableRenderer tableRenderer = new();

    [GeneratedRegex(@"^<!--\s*columns:\s*(\d+)\s*-->$", RegexOptions.IgnoreCase)]
    private static partial Regex ColumnStartRegex();

    [GeneratedRegex(@"^<!--\s*/columns\s*-->$", RegexOptions.IgnoreCase)]
    private static partial Regex ColumnEndRegex();

    public PdfRenderer()
    {
        FontSetup.EnsureInitialized();
    }

    /// <inheritdoc />
    public PdfDocument Render(MarkdownDocument document, LayoutOptions? layout = null)
    {
        var pdf = new PdfDocument();
        var context = new RenderContext(pdf, layout);

        var blocks = document.ToList();
        var i = 0;
        while (i < blocks.Count)
        {
            if (blocks[i] is HtmlBlock htmlBlock)
            {
                i = HandleHtmlBlock(htmlBlock, blocks, i, context);
                continue;
            }

            RenderBlock(blocks[i], blocks, i, context);
            i++;
        }

        context.Graphics.Dispose();
        AddPageNumbers(pdf, context.Layout);

        return pdf;
    }

    /// <summary>
    /// Draws page numbers in the bottom margin of every page:
    /// right-aligned on odd pages, left-aligned on even pages.
    /// </summary>
    private static void AddPageNumbers(PdfDocument pdf, LayoutOptions layout)
    {
        var font = new XFont(layout.BodyFontFamily, layout.PageNumberFontSize);
        var pageNumberY = layout.PageHeight - layout.Margin
                          + (layout.Margin - layout.PageNumberFontSize) / 2;

        for (var p = 0; p < pdf.PageCount; p++)
        {
            var page = pdf.Pages[p];
            using var gfx = XGraphics.FromPdfPage(page);

            var pageNumber = (p + 1).ToString();
            var isOddPage = (p + 1) % 2 != 0;

            if (isOddPage)
            {
                var textWidth = gfx.MeasureString(pageNumber, font).Width;
                var x = layout.Margin + layout.ContentWidth - textWidth;
                gfx.DrawString(pageNumber, font, XBrushes.Black,
                    new XPoint(x, pageNumberY + layout.PageNumberFontSize));
            }
            else
            {
                gfx.DrawString(pageNumber, font, XBrushes.Black,
                    new XPoint(layout.Margin, pageNumberY + layout.PageNumberFontSize));
            }
        }
    }

    private int HandleHtmlBlock(HtmlBlock htmlBlock, List<Block> blocks, int index, RenderContext context)
    {
        var content = htmlBlock.Lines.ToString().Trim();

        if (TryParseColumnStart(content, out var columnCount))
        {
            return RenderColumnSection(blocks, index, columnCount, context);
        }

        // Silently ignore other HTML blocks (comments, non-renderable HTML)
        return index + 1;
    }

    private int RenderColumnSection(List<Block> blocks, int startIndex, int columnCount, RenderContext context)
    {
        // Collect all blocks until matching <!-- /columns -->, tracking nesting depth
        var columnBlocks = new List<Block>();
        var i = startIndex + 1;
        var depth = 1;

        while (i < blocks.Count)
        {
            if (blocks[i] is HtmlBlock html)
            {
                var content = html.Lines.ToString().Trim();
                if (TryParseColumnStart(content, out _))
                {
                    depth++;
                }
                else if (ColumnEndRegex().IsMatch(content))
                {
                    depth--;
                    if (depth == 0)
                    {
                        i++;
                        break;
                    }
                }
            }

            columnBlocks.Add(blocks[i]);
            i++;
        }

        if (columnBlocks.Count == 0)
            return i;

        // Pre-process to identify nested column sections
        var items = PreprocessColumnBlocks(columnBlocks);

        // Compute column geometry
        var baseLeft = context.ContentLeft;
        var fullContentWidth = context.ContentWidth;
        var gutterWidth = context.Layout.ColumnGutter;
        var totalGutterWidth = (columnCount - 1) * gutterWidth;
        var columnWidth = (fullContentWidth - totalGutterWidth) / columnCount;

        // Measure all item heights at column width
        context.SetColumnLayout(baseLeft, columnWidth);
        var itemHeights = items.Select(item => MeasureColumnItemHeight(item, context)).ToList();
        context.ClearColumnLayout();

        // Distribute and render across pages
        DistributeAndRenderColumns(
            items, itemHeights, columnCount,
            columnWidth, gutterWidth, baseLeft, context);

        return i;
    }

    /// <summary>
    /// Represents a distributable unit within a column section — either a single
    /// Markdown block or a nested column section containing its own blocks.
    /// </summary>
    private record ColumnItem(List<Block> Blocks, int NestedColumnCount = 0)
    {
        public bool IsNestedSection => NestedColumnCount >= 2;
        public Block SingleBlock => Blocks[0];
    }

    /// <summary>
    /// Pre-processes a flat block list (which may contain inner column directive
    /// HtmlBlocks) into distributable items, grouping nested column sections.
    /// </summary>
    private static List<ColumnItem> PreprocessColumnBlocks(List<Block> flatBlocks)
    {
        var items = new List<ColumnItem>();
        var i = 0;

        while (i < flatBlocks.Count)
        {
            if (flatBlocks[i] is HtmlBlock html)
            {
                var content = html.Lines.ToString().Trim();
                if (TryParseColumnStart(content, out var innerCount))
                {
                    var innerBlocks = new List<Block>();
                    var depth = 1;
                    i++;
                    while (i < flatBlocks.Count)
                    {
                        if (flatBlocks[i] is HtmlBlock innerHtml)
                        {
                            var innerContent = innerHtml.Lines.ToString().Trim();
                            if (TryParseColumnStart(innerContent, out _))
                                depth++;
                            else if (ColumnEndRegex().IsMatch(innerContent))
                            {
                                depth--;
                                if (depth == 0) { i++; break; }
                            }
                        }

                        innerBlocks.Add(flatBlocks[i]);
                        i++;
                    }

                    items.Add(new ColumnItem(innerBlocks, innerCount));
                    continue;
                }
            }

            items.Add(new ColumnItem([flatBlocks[i]]));
            i++;
        }

        return items;
    }

    /// <summary>
    /// A contiguous sequence of items that must stay together in one column.
    /// </summary>
    private record struct KeepGroup(int StartIndex, int ItemCount, double TotalHeight);

    /// <summary>
    /// Builds atomic keep-groups from the item list so that:
    /// - A heading always stays with the item that follows it.
    /// - A short paragraph (1–2 lines) stays with a following table or list.
    /// - Nested column sections are never grouped with adjacent items.
    /// </summary>
    private List<KeepGroup> BuildKeepGroups(
        List<ColumnItem> items, List<double> itemHeights, RenderContext context)
    {
        var groups = new List<KeepGroup>();
        var i = 0;

        while (i < items.Count)
        {
            var count = 1;
            var height = itemHeights[i];

            if (i + 1 < items.Count && !items[i].IsNestedSection && !items[i + 1].IsNestedSection)
            {
                var current = items[i].SingleBlock;
                var next = items[i + 1].SingleBlock;

                if (current is HeadingBlock)
                {
                    count = 2;
                    height += itemHeights[i + 1];
                }
                else if (current is ParagraphBlock para && next is Table or ListBlock)
                {
                    if (paragraphRenderer.CountLines(para, context) <= 2)
                    {
                        count = 2;
                        height += itemHeights[i + 1];
                    }
                }
            }

            groups.Add(new KeepGroup(i, count, height));
            i += count;
        }

        return groups;
    }

    /// <summary>
    /// Distributes items across columns with balancing when all content fits on
    /// the current page, and page-by-page overflow when it does not.
    /// Keep-groups are treated as atomic units so related content is never split.
    /// </summary>
    private void DistributeAndRenderColumns(
        List<ColumnItem> allItems,
        List<double> itemHeights,
        int columnCount,
        double columnWidth,
        double gutterWidth,
        double baseLeft,
        RenderContext context)
    {
        // Build keep-groups at column width
        context.SetColumnLayout(baseLeft, columnWidth);
        var groups = BuildKeepGroups(allItems, itemHeights, context);
        context.ClearColumnLayout();

        var groupIndex = 0;

        while (groupIndex < groups.Count)
        {
            var remainingTotalHeight = 0.0;
            for (var k = groupIndex; k < groups.Count; k++)
                remainingTotalHeight += groups[k].TotalHeight;

            var availableHeight = context.RemainingHeight;
            var balancedTarget = remainingTotalHeight / columnCount;
            var isBalancing = balancedTarget <= availableHeight;
            var columnTarget = isBalancing ? balancedTarget : availableHeight;

            // Distribute groups into column buckets
            var columnBuckets = new List<List<ColumnItem>>();
            for (var c = 0; c < columnCount; c++)
                columnBuckets.Add([]);

            var currentColumn = 0;
            var currentHeight = 0.0;
            var consumed = 0;

            for (var gIdx = groupIndex; gIdx < groups.Count; gIdx++)
            {
                var group = groups[gIdx];
                var gh = group.TotalHeight;

                if (currentHeight > 0 && currentHeight + gh > columnTarget)
                {
                    if (currentColumn < columnCount - 1)
                    {
                        if (isBalancing)
                        {
                            var undershoot = columnTarget - currentHeight;
                            var overshoot = currentHeight + gh - columnTarget;
                            if (undershoot <= overshoot)
                            {
                                currentColumn++;
                                currentHeight = 0;
                            }
                        }
                        else
                        {
                            currentColumn++;
                            currentHeight = 0;
                        }
                    }
                    else if (!isBalancing)
                    {
                        break;
                    }
                }

                // Add all items in this group to the current column
                for (var idx = group.StartIndex; idx < group.StartIndex + group.ItemCount; idx++)
                {
                    columnBuckets[currentColumn].Add(allItems[idx]);
                }

                currentHeight += gh;
                consumed++;
            }

            // Render column buckets for this page
            var startY = context.CurrentY;
            var maxY = startY;

            for (var c = 0; c < columnCount; c++)
            {
                if (columnBuckets[c].Count == 0)
                    continue;

                var colLeft = baseLeft + c * (columnWidth + gutterWidth);
                context.SetColumnLayout(colLeft, columnWidth);
                context.CurrentY = startY;

                foreach (var item in columnBuckets[c])
                {
                    RenderColumnItem(item, context);
                }

                if (context.CurrentY > maxY)
                    maxY = context.CurrentY;
            }

            context.ClearColumnLayout();
            context.CurrentY = maxY;
            groupIndex += consumed;

            if (groupIndex < groups.Count)
            {
                context.AddPage();
            }
        }
    }

    private double MeasureBlocksHeight(List<Block> blocks, RenderContext context)
    {
        var totalHeight = 0.0;
        foreach (var block in blocks)
        {
            totalHeight += MeasureBlockHeight(block, context);
        }

        return totalHeight;
    }

    private double MeasureBlockHeight(Block block, RenderContext context) =>
        block switch
        {
            HeadingBlock h => headingRenderer.MeasureHeight(h, context),
            ParagraphBlock p => paragraphRenderer.MeasureHeight(p, context),
            ThematicBreakBlock t => thematicBreakRenderer.MeasureHeight(t, context),
            ListBlock l => listRenderer.MeasureHeight(l, context),
            QuoteBlock q => quoteBlockRenderer.MeasureHeight(q, context),
            Table tbl => tableRenderer.MeasureHeight(tbl, context),
            HtmlBlock => 0,
            _ => throw new UnsupportedMarkdownException(block.GetType().Name, block.Line + 1, block.Column + 1)
        };

    private double MeasureColumnItemHeight(ColumnItem item, RenderContext context)
    {
        if (!item.IsNestedSection)
            return MeasureBlockHeight(item.SingleBlock, context);

        return MeasureNestedSectionHeight(item.Blocks, item.NestedColumnCount, context);
    }

    /// <summary>
    /// Measures the rendered height of a nested column section by simulating
    /// balanced distribution at the inner column width and returning the tallest column.
    /// </summary>
    private double MeasureNestedSectionHeight(
        List<Block> innerBlocks, int columnCount, RenderContext context)
    {
        if (innerBlocks.Count == 0) return 0;

        var outerWidth = context.ContentWidth;
        var outerLeft = context.ContentLeft;
        var gutterWidth = context.Layout.ColumnGutter;
        var totalGutter = (columnCount - 1) * gutterWidth;
        var innerColWidth = (outerWidth - totalGutter) / columnCount;

        // Measure inner blocks at inner column width
        context.SetColumnLayout(outerLeft, innerColWidth);
        var innerHeights = innerBlocks.Select(b => MeasureBlockHeight(b, context)).ToList();
        context.SetColumnLayout(outerLeft, outerWidth); // restore

        // Simulate balanced distribution to find tallest column
        var totalHeight = innerHeights.Sum();
        var balancedTarget = totalHeight / columnCount;

        var columnHeights = new double[columnCount];
        var currentColumn = 0;

        for (var idx = 0; idx < innerBlocks.Count; idx++)
        {
            var bh = innerHeights[idx];
            if (columnHeights[currentColumn] > 0 &&
                columnHeights[currentColumn] + bh > balancedTarget &&
                currentColumn < columnCount - 1)
            {
                var undershoot = balancedTarget - columnHeights[currentColumn];
                var overshoot = columnHeights[currentColumn] + bh - balancedTarget;
                if (undershoot <= overshoot)
                    currentColumn++;
            }

            columnHeights[currentColumn] += bh;
        }

        return columnHeights.Max();
    }

    private void RenderColumnItem(ColumnItem item, RenderContext context)
    {
        if (item.IsNestedSection)
        {
            RenderNestedColumnSection(item.Blocks, item.NestedColumnCount, context);
            return;
        }

        RenderBlock(item.SingleBlock, [item.SingleBlock], 0, context);
    }

    /// <summary>
    /// Renders a nested column section within the current column layout.
    /// Uses the current content width as the available space for inner columns.
    /// </summary>
    private void RenderNestedColumnSection(
        List<Block> innerBlocks, int columnCount, RenderContext context)
    {
        if (innerBlocks.Count == 0) return;

        var baseLeft = context.ContentLeft;
        var fullWidth = context.ContentWidth;
        var gutterWidth = context.Layout.ColumnGutter;
        var totalGutter = (columnCount - 1) * gutterWidth;
        var innerColWidth = (fullWidth - totalGutter) / columnCount;

        // Measure inner blocks at inner column width
        context.SetColumnLayout(baseLeft, innerColWidth);
        var innerHeights = innerBlocks.Select(b => MeasureBlockHeight(b, context)).ToList();

        // Simulate balanced distribution
        var totalHeight = innerHeights.Sum();
        var balancedTarget = totalHeight / columnCount;

        var columnBuckets = new List<List<Block>>();
        for (var c = 0; c < columnCount; c++)
            columnBuckets.Add([]);

        var currentColumn = 0;
        var currentHeight = 0.0;

        for (var idx = 0; idx < innerBlocks.Count; idx++)
        {
            var bh = innerHeights[idx];
            if (currentHeight > 0 &&
                currentHeight + bh > balancedTarget &&
                currentColumn < columnCount - 1)
            {
                var undershoot = balancedTarget - currentHeight;
                var overshoot = currentHeight + bh - balancedTarget;
                if (undershoot <= overshoot)
                {
                    currentColumn++;
                    currentHeight = 0;
                }
            }

            columnBuckets[currentColumn].Add(innerBlocks[idx]);
            currentHeight += bh;
        }

        // Render inner columns
        var startY = context.CurrentY;
        var maxY = startY;

        for (var c = 0; c < columnCount; c++)
        {
            if (columnBuckets[c].Count == 0) continue;

            var colLeft = baseLeft + c * (innerColWidth + gutterWidth);
            context.SetColumnLayout(colLeft, innerColWidth);
            context.CurrentY = startY;

            foreach (var block in columnBuckets[c])
            {
                RenderBlock(block, [block], 0, context);
            }

            if (context.CurrentY > maxY)
                maxY = context.CurrentY;
        }

        // Restore outer column layout
        context.SetColumnLayout(baseLeft, fullWidth);
        context.CurrentY = maxY;
    }

    private static bool TryParseColumnStart(string content, out int columnCount)
    {
        var match = ColumnStartRegex().Match(content);
        if (match.Success && int.TryParse(match.Groups[1].Value, out columnCount) && columnCount >= 2)
        {
            return true;
        }

        columnCount = 0;
        return false;
    }

    private void RenderBlock(Block block, List<Block> blocks, int index, RenderContext context)
    {
        switch (block)
        {
            case HeadingBlock heading:
                headingRenderer.Render(heading, blocks, index, context);
                break;
            case ParagraphBlock paragraph:
                EnsureShortParagraphKeepsWithNext(paragraph, blocks, index, context);
                paragraphRenderer.Render(paragraph, context);
                break;
            case ThematicBreakBlock thematicBreak:
                thematicBreakRenderer.Render(thematicBreak, context);
                break;
            case ListBlock list:
                listRenderer.Render(list, context);
                break;
            case QuoteBlock quote:
                quoteBlockRenderer.Render(quote, context);
                break;
            case Table table:
                tableRenderer.Render(table, context);
                break;
            case HtmlBlock:
                // Non-directive HTML blocks inside column sections — silently ignore
                break;
            default:
                throw new UnsupportedMarkdownException(block.GetType().Name, block.Line + 1, block.Column + 1);
        }
    }

    /// <summary>
    /// When a short paragraph (1–2 lines) immediately precedes a table or list,
    /// ensures both elements appear on the same page/column.
    /// </summary>
    private void EnsureShortParagraphKeepsWithNext(
        ParagraphBlock paragraph, List<Block> blocks, int index, RenderContext context)
    {
        if (context.IsInColumnLayout)
            return;

        if (index + 1 >= blocks.Count)
            return;

        var nextBlock = blocks[index + 1];
        if (nextBlock is not (ListBlock or Table))
            return;

        var lineCount = paragraphRenderer.CountLines(paragraph, context);
        if (lineCount > 2)
            return;

        var paragraphHeight = paragraphRenderer.MeasureHeight(paragraph, context);
        var nextHeight = MeasureBlockHeight(nextBlock, context);
        context.EnsureSpace(paragraphHeight + nextHeight);
    }
}
