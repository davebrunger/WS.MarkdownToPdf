using System.Text.RegularExpressions;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
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
    public PdfDocument Render(MarkdownDocument document)
    {
        var pdf = new PdfDocument();
        var context = new RenderContext(pdf);

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

        return pdf;
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
        // Collect all blocks until <!-- /columns -->
        var columnBlocks = new List<Block>();
        var i = startIndex + 1;

        while (i < blocks.Count)
        {
            if (blocks[i] is HtmlBlock html)
            {
                var content = html.Lines.ToString().Trim();
                if (ColumnEndRegex().IsMatch(content))
                {
                    i++;
                    break;
                }
            }

            columnBlocks.Add(blocks[i]);
            i++;
        }

        if (columnBlocks.Count == 0)
            return i;

        // Compute column geometry
        var baseLeft = context.ContentLeft;
        var fullContentWidth = context.ContentWidth;
        var gutterWidth = LayoutConstants.ColumnGutter;
        var totalGutterWidth = (columnCount - 1) * gutterWidth;
        var columnWidth = (fullContentWidth - totalGutterWidth) / columnCount;

        // Measure all block heights at column width
        context.SetColumnLayout(baseLeft, columnWidth);
        var blockHeights = columnBlocks.Select(b => MeasureBlockHeight(b, context)).ToList();
        context.ClearColumnLayout();


        // Distribute and render across pages
        DistributeAndRenderColumns(
            columnBlocks, blockHeights, columnCount,
            columnWidth, gutterWidth, baseLeft, context);

        return i;
    }

    /// <summary>
    /// A contiguous sequence of blocks that must stay together in one column.
    /// </summary>
    private record struct KeepGroup(int StartIndex, int BlockCount, double TotalHeight);

    /// <summary>
    /// Builds atomic keep-groups from the block list so that:
    /// - A heading always stays with the block that follows it.
    /// - A short paragraph (1–2 lines) stays with a following table or list.
    /// </summary>
    private List<KeepGroup> BuildKeepGroups(
        List<Block> blocks, List<double> blockHeights, RenderContext context)
    {
        var groups = new List<KeepGroup>();
        var i = 0;

        while (i < blocks.Count)
        {
            var count = 1;
            var height = blockHeights[i];

            if (i + 1 < blocks.Count)
            {
                var current = blocks[i];
                var next = blocks[i + 1];

                if (current is HeadingBlock)
                {
                    count = 2;
                    height += blockHeights[i + 1];
                }
                else if (current is ParagraphBlock para && next is Table or ListBlock)
                {
                    if (paragraphRenderer.CountLines(para, context) <= 2)
                    {
                        count = 2;
                        height += blockHeights[i + 1];
                    }
                }
            }

            groups.Add(new KeepGroup(i, count, height));
            i += count;
        }

        return groups;
    }

    /// <summary>
    /// Distributes blocks across columns with balancing when all content fits on
    /// the current page, and page-by-page overflow when it does not.
    /// Keep-groups are treated as atomic units so related content is never split.
    /// </summary>
    private void DistributeAndRenderColumns(
        List<Block> allBlocks,
        List<double> blockHeights,
        int columnCount,
        double columnWidth,
        double gutterWidth,
        double baseLeft,
        RenderContext context)
    {
        // Build keep-groups at column width
        context.SetColumnLayout(baseLeft, columnWidth);
        var groups = BuildKeepGroups(allBlocks, blockHeights, context);
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
            var columnGroups = new List<List<Block>>();
            for (var c = 0; c < columnCount; c++)
                columnGroups.Add([]);

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

                // Add all blocks in this group to the current column
                for (var b = group.StartIndex; b < group.StartIndex + group.BlockCount; b++)
                {
                    columnGroups[currentColumn].Add(allBlocks[b]);
                }

                currentHeight += gh;
                consumed++;
            }

            // Render column groups for this page
            var startY = context.CurrentY;
            var maxY = startY;

            for (var c = 0; c < columnCount; c++)
            {
                if (columnGroups[c].Count == 0)
                    continue;

                var colLeft = baseLeft + c * (columnWidth + gutterWidth);
                context.SetColumnLayout(colLeft, columnWidth);
                context.CurrentY = startY;

                var colBlocks = columnGroups[c];
                for (var b = 0; b < colBlocks.Count; b++)
                {
                    RenderBlock(colBlocks[b], colBlocks, b, context);
                }

                if (context.CurrentY > maxY)
                    maxY = context.CurrentY;
            }

            context.ClearColumnLayout();
            context.CurrentY = maxY;

            // Advance by the total number of blocks consumed (not groups)
            var blocksConsumed = 0;
            for (var g = groupIndex; g < groupIndex + consumed; g++)
                blocksConsumed += groups[g].BlockCount;

            groupIndex += consumed;

            // Adjust blockHeights isn't needed — groups reference allBlocks by index

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
