using Markdig.Syntax;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering.BlockRenderers;

/// <summary>
/// Renders a <see cref="QuoteBlock"/> with indentation and a grey left bar (single level only).
/// </summary>
public class QuoteBlockRenderer
{
    private readonly InlineRenderer inlineRenderer = new();

    public void Render(QuoteBlock quote, RenderContext context)
    {
        ValidateSingleLevel(quote);

        var height = MeasureHeight(quote, context);
        context.EnsureSpace(height);

        var barX = context.ContentLeft + LayoutConstants.BlockQuoteBarGap;
        var textX = context.ContentLeft + LayoutConstants.BlockQuoteIndent;
        var barPen = new XPen(XColors.LightGray, LayoutConstants.BlockQuoteBarWidth);

        // Draw the left bar
        context.Graphics.DrawLine(
            barPen,
            barX, context.CurrentY,
            barX, context.CurrentY + height);

        // Render each paragraph inside the quote
        foreach (var child in quote.OfType<ParagraphBlock>())
        {
            if (child.Inline is null) continue;

            var runs = inlineRenderer.GetTextRuns(child.Inline, LayoutConstants.BodyFontSize);
            var currentX = textX;
            foreach (var run in runs)
            {
                context.Graphics.DrawString(
                    run.Text,
                    run.Font,
                    XBrushes.Black,
                    new XPoint(currentX, context.CurrentY + run.Font.Size));

                if (run.IsStrikethrough)
                {
                    var size = context.Graphics.MeasureString(run.Text, run.Font);
                    var strikeY = context.CurrentY + run.Font.Size - (run.Font.Size * 0.35);
                    context.Graphics.DrawLine(
                        XPens.Black,
                        currentX, strikeY,
                        currentX + size.Width, strikeY);
                }

                currentX += context.Graphics.MeasureString(run.Text, run.Font).Width;
            }

            context.CurrentY += LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        }

        context.CurrentY += LayoutConstants.ParagraphSpacing;
    }

    public double MeasureHeight(QuoteBlock quote, RenderContext context)
    {
        var paragraphCount = quote.OfType<ParagraphBlock>().Count();
        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        return paragraphCount * lineHeight + LayoutConstants.ParagraphSpacing;
    }

    private static void ValidateSingleLevel(QuoteBlock quote)
    {
        if (quote.Any(child => child is QuoteBlock))
        {
            throw new UnsupportedMarkdownException("Nested block quotes are not supported");
        }
    }
}
