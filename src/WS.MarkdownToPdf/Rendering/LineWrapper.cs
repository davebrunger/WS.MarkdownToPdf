using PdfSharp.Drawing;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering;

/// <summary>
/// Splits a list of text runs into wrapped lines that fit within a given width.
/// Respects explicit line breaks (<see cref="TextRun.IsLineBreak"/>) and wraps
/// on word boundaries when a line would exceed the available width.
/// </summary>
public static class LineWrapper
{
    /// <summary>
    /// Wraps text runs into lines that fit within <paramref name="availableWidth"/>.
    /// </summary>
    public static List<List<TextRun>> WrapLines(List<TextRun> runs, XGraphics graphics, double availableWidth)
    {
        var lines = new List<List<TextRun>>();
        var currentLine = new List<TextRun>();
        var currentLineWidth = 0.0;

        foreach (var run in runs)
        {
            if (run.IsLineBreak)
            {
                lines.Add(currentLine);
                currentLine = [];
                currentLineWidth = 0;
                continue;
            }

            var segments = SplitIntoWordSegments(run.Text);

            if (segments.Count == 0)
                continue;

            var accumulated = "";

            foreach (var segment in segments)
            {
                var testText = accumulated + segment;
                var testWidth = graphics.MeasureString(testText, run.Font).Width;

                if (currentLineWidth + testWidth > availableWidth
                    && (currentLine.Count > 0 || accumulated.Length > 0))
                {
                    // Flush what we've accumulated so far
                    if (accumulated.Length > 0)
                    {
                        currentLine.Add(run with { Text = accumulated.TrimEnd() });
                    }

                    lines.Add(currentLine);
                    currentLine = [];
                    currentLineWidth = 0;
                    accumulated = segment.TrimStart();
                }
                else
                {
                    accumulated = testText;
                }
            }

            if (accumulated.Length > 0)
            {
                var width = graphics.MeasureString(accumulated, run.Font).Width;
                currentLine.Add(run with { Text = accumulated });
                currentLineWidth += width;
            }
        }

        if (currentLine.Count > 0)
        {
            lines.Add(currentLine);
        }

        // Ensure at least one line even if input was empty
        if (lines.Count == 0)
        {
            lines.Add([]);
        }

        return lines;
    }

    /// <summary>
    /// Splits text into segments at word boundaries, keeping trailing spaces with the preceding word.
    /// "hello world foo" → ["hello ", "world ", "foo"]
    /// </summary>
    internal static List<string> SplitIntoWordSegments(string text)
    {
        var segments = new List<string>();
        var startIndex = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == ' ')
            {
                segments.Add(text[startIndex..(i + 1)]);
                startIndex = i + 1;
            }
        }

        if (startIndex < text.Length)
        {
            segments.Add(text[startIndex..]);
        }

        return segments;
    }
}
