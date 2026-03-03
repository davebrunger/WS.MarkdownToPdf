using Markdig.Syntax;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering.BlockRenderers;

/// <summary>
/// Renders a <see cref="ParagraphBlock"/> as body text with word wrapping.
/// </summary>
public class ParagraphRenderer
{
    private readonly InlineRenderer inlineRenderer = new();

    public void Render(ParagraphBlock paragraph, RenderContext context)
    {
        var runs = inlineRenderer.GetTextRuns(paragraph.Inline!, LayoutConstants.BodyFontSize);
        runs = FlattenSoftLineBreaks(runs);
        var lines = LineWrapper.WrapLines(runs, context.Graphics, context.ContentWidth);
        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var height = lines.Count * lineHeight + LayoutConstants.ParagraphSpacing;

        context.EnsureSpace(height);

        foreach (var line in lines)
        {
            var currentX = context.ContentLeft;
            foreach (var run in line)
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

            context.CurrentY += lineHeight;
        }

        context.CurrentY += LayoutConstants.ParagraphSpacing;
    }

    public double MeasureHeight(ParagraphBlock paragraph, RenderContext context)
    {
        if (paragraph.Inline is null)
        {
            return LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier
                   + LayoutConstants.ParagraphSpacing;
        }

        var runs = inlineRenderer.GetTextRuns(paragraph.Inline, LayoutConstants.BodyFontSize);
        runs = FlattenSoftLineBreaks(runs);
        var lines = LineWrapper.WrapLines(runs, context.Graphics, context.ContentWidth);
        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        return lines.Count * lineHeight + LayoutConstants.ParagraphSpacing;
    }

    /// <summary>
    /// Returns the number of wrapped lines this paragraph would produce at the current content width.
    /// </summary>
    public int CountLines(ParagraphBlock paragraph, RenderContext context)
    {
        if (paragraph.Inline is null)
            return 1;

        var runs = inlineRenderer.GetTextRuns(paragraph.Inline, LayoutConstants.BodyFontSize);
        runs = FlattenSoftLineBreaks(runs);
        return LineWrapper.WrapLines(runs, context.Graphics, context.ContentWidth).Count;
    }

    /// <summary>
    /// Converts soft line breaks into spaces. In standard Markdown, a single newline
    /// within a paragraph is rendered as a space, not as a new line.
    /// </summary>
    private static List<TextRun> FlattenSoftLineBreaks(List<TextRun> runs) =>
        runs.Select(r => r.IsSoftLineBreak ? r with { Text = " ", IsSoftLineBreak = false } : r).ToList();
}
