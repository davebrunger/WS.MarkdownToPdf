using Markdig.Syntax;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Layout;

namespace WS.MarkdownToPdf.Rendering.BlockRenderers;

/// <summary>
/// Renders a <see cref="ThematicBreakBlock"/> as a horizontal line.
/// </summary>
public class ThematicBreakRenderer
{
    public void Render(ThematicBreakBlock thematicBreak, RenderContext context)
    {
        var height = MeasureHeight(thematicBreak, context);
        context.EnsureSpace(height);

        var pen = new XPen(XColors.Black, LayoutConstants.HorizontalRuleThickness);
        var y = context.CurrentY + (height / 2);
        context.Graphics.DrawLine(pen, context.ContentLeft, y, context.ContentRight, y);

        context.CurrentY += height;
    }

    public double MeasureHeight(ThematicBreakBlock thematicBreak, RenderContext context) =>
        LayoutConstants.BodyFontSize * LayoutConstants.LineSpacingMultiplier
        + LayoutConstants.ParagraphSpacing;
}
