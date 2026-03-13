using PdfSharp.Drawing;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Rendering;

/// <summary>
/// Post-processes wrapped lines to insert hyphenation when full justification
/// would produce word gaps exceeding <see cref="LayoutOptions.MaxJustificationGap"/>.
/// </summary>
internal static class LineHyphenator
{
    private const int MinFragmentLength = 3;

    /// <summary>
    /// Scans each non-last line that will be justified; if the extra-per-gap
    /// exceeds the configured maximum, splits the first word of the next line
    /// and pulls a hyphenated fragment back onto the current line.
    /// </summary>
    internal static void HyphenateIfNeeded(
        List<List<TextRun>> lines, XGraphics graphics, double availableWidth, LayoutOptions layout)
    {
        for (var i = lines.Count - 2; i >= 0; i--)
        {
            var line = lines[i];
            var nextLine = lines[i + 1];
            if (line.Count == 0 || nextLine.Count == 0) continue;

            var extraPerGap = MeasureExtraPerGap(line, graphics, availableWidth);
            if (extraPerGap <= layout.MaxJustificationGap) continue;

            TryPullFromNextLine(line, nextLine, lines, i, graphics, availableWidth, layout);
        }
    }

    /// <summary>
    /// Attempts to hyphenate the first word of the next line and pull the longest
    /// valid fragment back onto the current line.
    /// </summary>
    private static bool TryPullFromNextLine(
        List<TextRun> line, List<TextRun> nextLine, List<List<TextRun>> lines,
        int lineIndex, XGraphics graphics, double availableWidth, LayoutOptions layout)
    {
        var firstRun = nextLine[0];
        var word = firstRun.Text.TrimStart();
        if (word.Length < MinFragmentLength * 2) return false;

        var lineWidth = MeasureLineWidth(line, graphics);
        var spaceWidth = graphics.MeasureString(" ", firstRun.Font).Width;

        // Iterate from longest to shortest fragment — take the first valid split
        // that brings the gap within threshold
        var fallbackPos = -1;
        for (var pos = word.Length - MinFragmentLength; pos >= MinFragmentLength; pos--)
        {
            if (!IsGoodSplitPoint(word, pos)) continue;

            var fragment = word[..pos] + "-";
            var fragmentWidth = graphics.MeasureString(fragment, firstRun.Font).Width;

            if (lineWidth + spaceWidth + fragmentWidth > availableWidth) continue;

            // Track longest fitting fragment as fallback (first one we encounter)
            if (fallbackPos < 0)
                fallbackPos = pos;

            var newGapCount = CountGaps(line) + 1;
            var newExtra = (availableWidth - lineWidth - spaceWidth - fragmentWidth) / newGapCount;

            if (newExtra > layout.MaxJustificationGap) continue;

            // Under threshold — accept immediately
            fallbackPos = pos;
            break;
        }

        if (fallbackPos < 0) return false;

        var pulledFragment = word[..fallbackPos] + "-";
        var remainder = word[fallbackPos..];
        line.Add(firstRun with { Text = " " + pulledFragment });

        var leadingWhitespace = firstRun.Text[..^word.Length];
        if (remainder.Length > 0)
            nextLine[0] = firstRun with { Text = leadingWhitespace + remainder };
        else
            nextLine.RemoveAt(0);

        if (nextLine.Count == 0)
            lines.RemoveAt(lineIndex + 1);

        return true;
    }

    private static double MeasureExtraPerGap(List<TextRun> line, XGraphics graphics, double availableWidth)
    {
        var naturalWidth = MeasureLineWidth(line, graphics);
        var gapCount = CountGaps(line);
        if (gapCount == 0) return 0;
        return (availableWidth - naturalWidth) / gapCount;
    }

    private static double MeasureLineWidth(List<TextRun> line, XGraphics graphics)
    {
        var width = 0.0;
        foreach (var run in line)
            width += graphics.MeasureString(run.Text, run.Font).Width;
        return width;
    }

    private static int CountGaps(List<TextRun> line)
    {
        var count = 0;
        foreach (var run in line)
            count += CountGapsInText(run.Text);
        return count;
    }

    private static int CountGapsInText(string text)
    {
        var count = 0;
        var segments = LineWrapper.SplitIntoWordSegments(text);
        foreach (var seg in segments)
        {
            if (seg.EndsWith(' '))
                count++;
        }
        return count;
    }

    private static bool IsGoodSplitPoint(string word, int pos)
    {
        if (pos < MinFragmentLength || pos > word.Length - MinFragmentLength) return false;

        var before = char.ToLowerInvariant(word[pos - 1]);
        var after = char.ToLowerInvariant(word[pos]);

        if (IsVowel(before) && !IsVowel(after)) return true;
        if (!IsVowel(before) && !IsVowel(after)) return true;

        return false;
    }

    private static bool IsVowel(char c) => "aeiou".Contains(c);
}
