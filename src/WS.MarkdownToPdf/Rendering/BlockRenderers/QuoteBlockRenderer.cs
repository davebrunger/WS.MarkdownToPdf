using Markdig.Syntax;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering.BlockRenderers;

/// <summary>
/// Renders a <see cref="QuoteBlock"/> with indentation, a grey left bar, word wrapping,
/// and line-break support (single level only).
/// </summary>
public class QuoteBlockRenderer
{
    private readonly InlineRenderer inlineRenderer = new();

    public void Render(QuoteBlock quote, RenderContext context)
    {
        ValidateSingleLevel(quote);

        var height = MeasureHeight(quote, context);
        context.EnsureSpace(height);

        var barX = context.ContentLeft + context.Layout.BlockQuoteBarGap;
        var textX = context.ContentLeft + context.Layout.BlockQuoteIndent;
        var barPen = new XPen(ParseHexColor(context.Layout.BlockQuoteBarColor), context.Layout.BlockQuoteBarWidth);
        var lineHeight = context.Layout.BodyFontSize * context.Layout.LineSpacingMultiplier;
        var availableWidth = context.ContentWidth - context.Layout.BlockQuoteIndent;

        // Draw the left bar
        context.Graphics.DrawLine(
            barPen,
            barX, context.CurrentY,
            barX, context.CurrentY + height);

        // Render each paragraph inside the quote
        foreach (var child in quote.OfType<ParagraphBlock>())
        {
            if (child.Inline is null) continue;

            var runs = inlineRenderer.GetTextRuns(child.Inline, context.Layout.BodyFontSize, context.Layout);
            runs = PromoteSoftLineBreaks(runs);
            var lines = LineWrapper.WrapLines(runs, context.Graphics, availableWidth);

            foreach (var line in lines)
            {
                var currentX = textX;
                foreach (var run in line)
                {
                    context.Graphics.DrawString(
                        run.Text,
                        run.Font,
                        XBrushes.Black,
                        new XPoint(currentX, context.CurrentY + run.Font.Size));

                    if (run.IsStrikethrough)
                    {
                        var size = context.Graphics.MeasureString(run.Text, run.Font);
                        var strikeY = context.CurrentY + run.Font.Size - (run.Font.Size * context.Layout.StrikethroughOffsetRatio);
                        context.Graphics.DrawLine(
                            XPens.Black,
                            currentX, strikeY,
                            currentX + size.Width, strikeY);
                    }

                    currentX += context.Graphics.MeasureString(run.Text, run.Font).Width;
                }

                context.CurrentY += lineHeight;
            }
        }

        context.CurrentY += context.Layout.ParagraphSpacing;
    }

    public double MeasureHeight(QuoteBlock quote, RenderContext context)
    {
        var lineHeight = context.Layout.BodyFontSize * context.Layout.LineSpacingMultiplier;
        var availableWidth = context.ContentWidth - context.Layout.BlockQuoteIndent;
        var totalLines = 0;

        foreach (var child in quote.OfType<ParagraphBlock>())
        {
            if (child.Inline is null) continue;

            var runs = inlineRenderer.GetTextRuns(child.Inline, context.Layout.BodyFontSize, context.Layout);
            runs = PromoteSoftLineBreaks(runs);
            var lines = LineWrapper.WrapLines(runs, context.Graphics, availableWidth);
            totalLines += lines.Count;
        }

        return Math.Max(totalLines, 1) * lineHeight + context.Layout.ParagraphSpacing;
    }

    /// <summary>
    /// In block quotes, each <c>&gt;</c> marker should start a new line.
    /// Soft line breaks (produced by continuation lines) are promoted to hard line breaks.
    /// </summary>
    private static List<TextRun> PromoteSoftLineBreaks(List<TextRun> runs) =>
        runs.Select(r => r.IsSoftLineBreak ? r with { IsSoftLineBreak = false, IsLineBreak = true } : r).ToList();

    private static XColor ParseHexColor(string hex)
    {
        var span = hex.AsSpan().TrimStart('#');
        var r = byte.Parse(span[..2], System.Globalization.NumberStyles.HexNumber);
        var g = byte.Parse(span[2..4], System.Globalization.NumberStyles.HexNumber);
        var b = byte.Parse(span[4..6], System.Globalization.NumberStyles.HexNumber);
        return XColor.FromArgb(r, g, b);
    }

    private static void ValidateSingleLevel(QuoteBlock quote)
    {
        var nestedQuote = quote.OfType<QuoteBlock>().FirstOrDefault();
        if (nestedQuote is not null)
        {
            throw new UnsupportedMarkdownException("Nested block quotes are not supported", nestedQuote.Line + 1, nestedQuote.Column + 1);
        }
    }
}
