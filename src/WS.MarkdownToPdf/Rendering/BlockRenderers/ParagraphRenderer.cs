using Markdig.Syntax;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering.BlockRenderers;

/// <summary>
/// Renders a <see cref="ParagraphBlock"/> as body text.
/// </summary>
public class ParagraphRenderer
{
    private readonly InlineRenderer inlineRenderer = new();

    public void Render(ParagraphBlock paragraph, RenderContext context)
    {
        var runs = inlineRenderer.GetTextRuns(paragraph.Inline!, LayoutConstants.BodyFontSize);
        var height = MeasureHeight(paragraph, context);

        context.EnsureSpace(height);

        var currentX = context.ContentLeft;
        foreach (var run in runs)
        {
            context.Graphics.DrawString(
                run.Text,
                run.Font,
                XBrushes.Black,
                new XPoint(currentX, context.CurrentY + run.Font.Size));

            var runWidth = context.Graphics.MeasureString(run.Text, run.Font).Width;

            if (run.IsStrikethrough)
            {
                var strikeY = context.CurrentY + run.Font.Size - (run.Font.Size * 0.35);
                context.Graphics.DrawLine(
                    XPens.Black,
                    currentX, strikeY,
                    currentX + runWidth, strikeY);
            }

            currentX += runWidth;
        }

        context.CurrentY += height;
    }

    public double MeasureHeight(ParagraphBlock paragraph, RenderContext context)
    {
        return LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier
               + LayoutConstants.ParagraphSpacing;
    }
}
