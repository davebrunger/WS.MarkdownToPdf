using Markdig.Syntax;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering.BlockRenderers;

/// <summary>
/// Renders a <see cref="HeadingBlock"/> with level-appropriate font size.
/// </summary>
public class HeadingRenderer
{
    private readonly InlineRenderer inlineRenderer = new();

    /// <summary>
    /// Renders a heading, ensuring it stays on the same page as the following block.
    /// </summary>
    public void Render(HeadingBlock heading, List<Block> blocks, int index, RenderContext context)
    {
        var fontSize = LayoutConstants.GetHeadingFontSize(heading.Level);
        var font = new XFont(LayoutConstants.BodyFontFamily, fontSize, XFontStyleEx.Bold);
        var headingHeight = MeasureHeight(heading, context);

        // Look ahead: heading must stay with the next block (skip in column layout
        // where the distribution algorithm handles page fitting)
        var combinedHeight = headingHeight + LayoutConstants.ParagraphSpacing;
        if (!context.IsInColumnLayout && index + 1 < blocks.Count)
        {
            combinedHeight += MeasureNextBlockHeight(blocks[index + 1], context);
        }

        context.EnsureSpace(combinedHeight);

        var runs = inlineRenderer.GetTextRuns(heading.Inline!, fontSize);
        var lines = LineWrapper.WrapLines(runs, context.Graphics, context.ContentWidth);
        var lineHeight = fontSize * LayoutConstants.LineSpacingMultiplier;

        foreach (var line in lines)
        {
            var currentX = context.ContentLeft;
            foreach (var run in line)
            {
                var runFont = new XFont(LayoutConstants.BodyFontFamily, fontSize, run.Font.Style);
                context.Graphics.DrawString(
                    run.Text,
                    runFont,
                    XBrushes.Black,
                    new XPoint(currentX, context.CurrentY + fontSize));

                currentX += context.Graphics.MeasureString(run.Text, runFont).Width;
            }

            context.CurrentY += lineHeight;
        }

        context.CurrentY += LayoutConstants.ParagraphSpacing;
    }

    public double MeasureHeight(HeadingBlock heading, RenderContext context)
    {
        var fontSize = LayoutConstants.GetHeadingFontSize(heading.Level);
        var lineHeight = fontSize * LayoutConstants.LineSpacingMultiplier;

        if (heading.Inline is null)
            return lineHeight + LayoutConstants.ParagraphSpacing;

        var runs = inlineRenderer.GetTextRuns(heading.Inline, fontSize);
        var lines = LineWrapper.WrapLines(runs, context.Graphics, context.ContentWidth);
        return lines.Count * lineHeight + LayoutConstants.ParagraphSpacing;
    }

    private double MeasureNextBlockHeight(Block nextBlock, RenderContext context) =>
        nextBlock switch
        {
            ParagraphBlock p => new ParagraphRenderer().MeasureHeight(p, context),
            ThematicBreakBlock t => new ThematicBreakRenderer().MeasureHeight(t, context),
            ListBlock l => new ListRenderer().MeasureHeight(l, context),
            QuoteBlock q => new QuoteBlockRenderer().MeasureHeight(q, context),
            Markdig.Extensions.Tables.Table tbl => new TableRenderer().MeasureHeight(tbl, context),
            HeadingBlock h => MeasureHeight(h, context),
            HtmlBlock => 0,
            _ => throw new UnsupportedMarkdownException(nextBlock.GetType().Name, nextBlock.Line + 1, nextBlock.Column + 1)
        };
}
