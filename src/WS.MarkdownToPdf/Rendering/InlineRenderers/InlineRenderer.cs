using Markdig.Syntax.Inlines;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Layout;

namespace WS.MarkdownToPdf.Rendering.InlineRenderers;

/// <summary>
/// Walks inline AST nodes and produces a list of <see cref="TextRun"/> objects
/// with appropriate fonts and styles.
/// </summary>
public class InlineRenderer
{
    /// <summary>
    /// Extracts styled text runs from the given inline container.
    /// </summary>
    public List<TextRun> GetTextRuns(ContainerInline container, double baseFontSize)
    {
        var runs = new List<TextRun>();
        CollectRuns(container, baseFontSize, XFontStyleEx.Regular, false, runs);
        return runs;
    }

    private void CollectRuns(
        Inline inline,
        double baseFontSize,
        XFontStyleEx currentStyle,
        bool isStrikethrough,
        List<TextRun> runs)
    {
        switch (inline)
        {
            case LiteralInline literal:
                if (literal.Content.Length > 0)
                {
                    var font = new XFont(LayoutConstants.BodyFontFamily, baseFontSize, currentStyle);
                    runs.Add(new TextRun(literal.Content.ToString(), font, isStrikethrough));
                }
                break;

            case EmphasisInline emphasis:
                var newStyle = currentStyle;
                var newStrikethrough = isStrikethrough;

                if (emphasis.DelimiterChar == '~' && emphasis.DelimiterCount == 2)
                {
                    newStrikethrough = true;
                }
                else if (emphasis.DelimiterCount == 2)
                {
                    newStyle |= XFontStyleEx.Bold;
                }
                else if (emphasis.DelimiterCount == 1)
                {
                    newStyle |= XFontStyleEx.Italic;
                }

                foreach (var child in emphasis)
                {
                    CollectRuns(child, baseFontSize, newStyle, newStrikethrough, runs);
                }
                break;

            case CodeInline code:
                var monoFont = new XFont(LayoutConstants.MonoFontFamily, baseFontSize, XFontStyleEx.Regular);
                runs.Add(new TextRun(code.Content, monoFont));
                break;

            case LineBreakInline lineBreak:
                var breakFont = new XFont(LayoutConstants.BodyFontFamily, baseFontSize, currentStyle);
                if (lineBreak.IsHard)
                {
                    runs.Add(new TextRun("", breakFont, IsLineBreak: true));
                }
                else
                {
                    runs.Add(new TextRun("", breakFont, IsSoftLineBreak: true));
                }
                break;

            case ContainerInline container:
                foreach (var child in container)
                {
                    CollectRuns(child, baseFontSize, currentStyle, isStrikethrough, runs);
                }
                break;

            default:
                throw new UnsupportedMarkdownException(inline.GetType().Name, inline.Line + 1, inline.Column + 1);
        }
    }
}
