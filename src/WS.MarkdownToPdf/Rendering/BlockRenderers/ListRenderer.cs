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
        var font = new XFont(LayoutConstants.BodyFontFamily, LayoutConstants.BodyFontSize);
        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;

        foreach (var item in list.OfType<ListItemBlock>())
        {
            var prefix = list.IsOrdered ? $"{itemNumber}. " : "\u2022 ";
            var prefixWidth = context.Graphics.MeasureString(prefix, font).Width;

            context.Graphics.DrawString(
                prefix,
                font,
                XBrushes.Black,
                new XPoint(context.ContentLeft, context.CurrentY + LayoutConstants.BodyFontSize));

            // Render inline content with word wrapping
            if (item.FirstOrDefault() is ParagraphBlock paragraph && paragraph.Inline is not null)
            {
                var runs = inlineRenderer.GetTextRuns(paragraph.Inline, LayoutConstants.BodyFontSize);
                var availableWidth = context.ContentWidth - prefixWidth;
                var lines = LineWrapper.WrapLines(runs, context.Graphics, availableWidth);
                var textX = context.ContentLeft + prefixWidth;

                for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
                {
                    var currentX = textX;
                    foreach (var run in lines[lineIndex])
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

                    context.CurrentY += lineHeight;
                }
            }
            else
            {
                context.CurrentY += lineHeight;
            }

            itemNumber++;
        }

        context.CurrentY += LayoutConstants.ParagraphSpacing;
    }

    public double MeasureHeight(ListBlock list, RenderContext context)
    {
        var font = new XFont(LayoutConstants.BodyFontFamily, LayoutConstants.BodyFontSize);
        var lineHeight = LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier;
        var totalHeight = 0.0;

        var itemNumber = 1;
        foreach (var item in list.OfType<ListItemBlock>())
        {
            var prefix = list.IsOrdered ? $"{itemNumber}. " : "\u2022 ";
            var prefixWidth = context.Graphics.MeasureString(prefix, font).Width;
            var availableWidth = context.ContentWidth - prefixWidth;

            if (item.FirstOrDefault() is ParagraphBlock paragraph && paragraph.Inline is not null)
            {
                var runs = inlineRenderer.GetTextRuns(paragraph.Inline, LayoutConstants.BodyFontSize);
                var lines = LineWrapper.WrapLines(runs, context.Graphics, availableWidth);
                totalHeight += lines.Count * lineHeight;
            }
            else
            {
                totalHeight += lineHeight;
            }

            itemNumber++;
        }

        return totalHeight + LayoutConstants.ParagraphSpacing;
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
