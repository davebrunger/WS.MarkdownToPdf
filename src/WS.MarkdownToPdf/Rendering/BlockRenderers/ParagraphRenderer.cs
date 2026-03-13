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
        var runs = inlineRenderer.GetTextRuns(paragraph.Inline!, context.Layout.BodyFontSize, context.Layout);
        runs = FlattenSoftLineBreaks(runs);
        var lines = LineWrapper.WrapLines(runs, context.Graphics, context.ContentWidth);
        LineHyphenator.HyphenateIfNeeded(lines, context.Graphics, context.ContentWidth, context.Layout);
        var lineHeight = context.Layout.BodyFontSize * context.Layout.LineSpacingMultiplier;
        var height = lines.Count * lineHeight + context.Layout.ParagraphSpacing;

        context.EnsureSpace(height);

        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var isLastLine = lineIndex == lines.Count - 1;
            JustifiedLineDrawer.DrawLine(
                lines[lineIndex], context.Graphics,
                context.ContentLeft, context.CurrentY,
                context.ContentWidth, context.Layout,
                justify: !isLastLine);

            context.CurrentY += lineHeight;
        }

        context.CurrentY += context.Layout.ParagraphSpacing;
    }

    public double MeasureHeight(ParagraphBlock paragraph, RenderContext context)
    {
        if (paragraph.Inline is null)
        {
            return context.Layout.BodyFontSize * context.Layout.LineSpacingMultiplier
                   + context.Layout.ParagraphSpacing;
        }

        var runs = inlineRenderer.GetTextRuns(paragraph.Inline, context.Layout.BodyFontSize, context.Layout);
        runs = FlattenSoftLineBreaks(runs);
        var lines = LineWrapper.WrapLines(runs, context.Graphics, context.ContentWidth);
        var lineHeight = context.Layout.BodyFontSize * context.Layout.LineSpacingMultiplier;
        return lines.Count * lineHeight + context.Layout.ParagraphSpacing;
    }

    /// <summary>
    /// Returns the number of wrapped lines this paragraph would produce at the current content width.
    /// </summary>
    public int CountLines(ParagraphBlock paragraph, RenderContext context)
    {
        if (paragraph.Inline is null)
            return 1;

        var runs = inlineRenderer.GetTextRuns(paragraph.Inline, context.Layout.BodyFontSize, context.Layout);
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
