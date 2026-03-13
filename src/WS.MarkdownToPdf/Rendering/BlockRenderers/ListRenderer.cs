using Markdig.Syntax;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering.BlockRenderers;

/// <summary>
/// Renders a <see cref="ListBlock"/> (bulleted or ordered, single level only).
/// </summary>
public class ListRenderer
{
    private readonly InlineRenderer inlineRenderer = new();

    public void Render(ListBlock list, RenderContext context)
    {
        ValidateSingleLevel(list);

        var height = MeasureHeight(list, context);
        context.EnsureSpace(height);

        var itemNumber = 1;
        var font = new XFont(context.Layout.BodyFontFamily, context.Layout.BodyFontSize);
        var lineHeight = context.Layout.BodyFontSize * context.Layout.LineSpacingMultiplier;

        foreach (var item in list.OfType<ListItemBlock>())
        {
            var prefix = list.IsOrdered ? $"{itemNumber}. " : "\u2022 ";
            var prefixWidth = context.Graphics.MeasureString(prefix, font).Width;

            context.Graphics.DrawString(
                prefix,
                font,
                XBrushes.Black,
                new XPoint(context.ContentLeft, context.CurrentY + context.Layout.BodyFontSize));

            // Render inline content with word wrapping
            if (item.FirstOrDefault() is ParagraphBlock paragraph && paragraph.Inline is not null)
            {
                var runs = inlineRenderer.GetTextRuns(paragraph.Inline, context.Layout.BodyFontSize, context.Layout);
                var availableWidth = context.ContentWidth - prefixWidth;
                var lines = LineWrapper.WrapLines(runs, context.Graphics, availableWidth);
                LineHyphenator.HyphenateIfNeeded(lines, context.Graphics, availableWidth, context.Layout);
                var textX = context.ContentLeft + prefixWidth;

                for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    var isLastLine = lineIndex == lines.Count - 1;
                    JustifiedLineDrawer.DrawLine(
                        lines[lineIndex], context.Graphics,
                        textX, context.CurrentY,
                        availableWidth, context.Layout,
                        justify: !isLastLine);

                    context.CurrentY += lineHeight;
                }
            }
            else
            {
                context.CurrentY += lineHeight;
            }

            context.CurrentY += context.Layout.ListItemSpacing;
            itemNumber++;
        }

        context.CurrentY += context.Layout.ParagraphSpacing;
    }

    public double MeasureHeight(ListBlock list, RenderContext context)
    {
        var font = new XFont(context.Layout.BodyFontFamily, context.Layout.BodyFontSize);
        var lineHeight = context.Layout.BodyFontSize * context.Layout.LineSpacingMultiplier;
        var totalHeight = 0.0;

        var itemNumber = 1;
        foreach (var item in list.OfType<ListItemBlock>())
        {
            var prefix = list.IsOrdered ? $"{itemNumber}. " : "\u2022 ";
            var prefixWidth = context.Graphics.MeasureString(prefix, font).Width;
            var availableWidth = context.ContentWidth - prefixWidth;

            if (item.FirstOrDefault() is ParagraphBlock paragraph && paragraph.Inline is not null)
            {
                var runs = inlineRenderer.GetTextRuns(paragraph.Inline, context.Layout.BodyFontSize, context.Layout);
                var lines = LineWrapper.WrapLines(runs, context.Graphics, availableWidth);
                totalHeight += lines.Count * lineHeight;
            }
            else
            {
                totalHeight += lineHeight;
            }

            totalHeight += context.Layout.ListItemSpacing;
            itemNumber++;
        }

        return totalHeight + context.Layout.ParagraphSpacing;
    }

    private static void ValidateSingleLevel(ListBlock list)
    {
        foreach (var item in list.OfType<ListItemBlock>())
        {
            var nestedList = item.OfType<ListBlock>().FirstOrDefault();
            if (nestedList is not null)
            {
                throw new UnsupportedMarkdownException("Nested lists are not supported", nestedList.Line + 1, nestedList.Column + 1);
            }
        }
    }
}
