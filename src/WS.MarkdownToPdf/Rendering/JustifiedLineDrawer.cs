using PdfSharp.Drawing;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering;

/// <summary>
/// Draws a line of text runs with full justification by distributing extra
/// horizontal space across word boundaries.
/// </summary>
internal static class JustifiedLineDrawer
{
    /// <summary>
    /// Draws a single line of runs. When <paramref name="justify"/> is true and
    /// the line contains at least two space-separated segments, extra space is
    /// distributed evenly between words.
    /// </summary>
    internal static void DrawLine(
        List<TextRun> runs,
        XGraphics graphics,
        double startX,
        double currentY,
        double availableWidth,
        LayoutOptions layout,
        bool justify)
    {
        if (runs.Count == 0) return;

        // Split each run into word segments for justification
        var segments = new List<(string Text, XFont Font, bool IsStrikethrough)>();
        foreach (var run in runs)
        {
            var words = LineWrapper.SplitIntoWordSegments(run.Text);
            if (words.Count == 0 && run.Text.Length > 0)
                words = [run.Text];

            foreach (var word in words)
                segments.Add((word, run.Font, run.IsStrikethrough));
        }

        // Measure natural width and count inter-word gaps
        var naturalWidth = 0.0;
        var gapCount = 0;
        for (var i = 0; i < segments.Count; i++)
        {
            naturalWidth += graphics.MeasureString(segments[i].Text, segments[i].Font).Width;
            if (i > 0 && segments[i].Text.Length > 0 && !segments[i].Text.StartsWith(' '))
                gapCount++;
        }

        // Count gaps from trailing spaces on segments (the more reliable measure)
        gapCount = 0;
        for (var i = 0; i < segments.Count; i++)
        {
            if (segments[i].Text.EndsWith(' '))
                gapCount++;
        }

        var extraPerGap = 0.0;
        if (justify && gapCount > 0 && availableWidth > naturalWidth)
        {
            extraPerGap = (availableWidth - naturalWidth) / gapCount;
        }

        var currentX = startX;
        foreach (var (text, font, isStrikethrough) in segments)
        {
            var displayText = text;
            var segWidth = graphics.MeasureString(displayText, font).Width;

            graphics.DrawString(
                displayText,
                font,
                XBrushes.Black,
                new XPoint(currentX, currentY + font.Size));

            if (isStrikethrough)
            {
                var strikeY = currentY + font.Size - (font.Size * layout.StrikethroughOffsetRatio);
                graphics.DrawLine(XPens.Black, currentX, strikeY, currentX + segWidth, strikeY);
            }

            currentX += segWidth;

            if (text.EndsWith(' '))
                currentX += extraPerGap;
        }
    }
}
