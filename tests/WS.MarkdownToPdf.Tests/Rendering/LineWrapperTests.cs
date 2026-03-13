using PdfSharp.Drawing;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class LineWrapperTests
{
    private readonly XGraphics graphics;

    public LineWrapperTests()
    {
        FontSetup.EnsureInitialized();
        var doc = new PdfDocument();
        var page = doc.AddPage();
        graphics = XGraphics.FromPdfPage(page);
    }

    [Fact]
    public void WrapLines_ShortText_ReturnsSingleLine()
    {
        var font = new XFont(LayoutConstants.BodyFontFamily, LayoutConstants.BodyFontSize);
        var runs = new List<TextRun> { new("Hello world", font) };

        var lines = LineWrapper.WrapLines(runs, graphics, LayoutConstants.ContentWidth);

        Assert.Single(lines);
        Assert.Equal("Hello world", string.Concat(lines[0].Select(r => r.Text)));
    }

    [Fact]
    public void WrapLines_LongText_WrapsToMultipleLines()
    {
        var font = new XFont(LayoutConstants.BodyFontFamily, LayoutConstants.BodyFontSize);
        var longText = string.Join(" ", Enumerable.Repeat("word", 80));
        var runs = new List<TextRun> { new(longText, font) };

        var lines = LineWrapper.WrapLines(runs, graphics, LayoutConstants.ContentWidth);

        Assert.True(lines.Count > 1);
    }

    [Fact]
    public void WrapLines_LineBreakRun_ForcesNewLine()
    {
        var font = new XFont(LayoutConstants.BodyFontFamily, LayoutConstants.BodyFontSize);
        var runs = new List<TextRun>
        {
            new("Line one", font),
            new("", font, IsLineBreak: true),
            new("Line two", font)
        };

        var lines = LineWrapper.WrapLines(runs, graphics, LayoutConstants.ContentWidth);

        Assert.Equal(2, lines.Count);
        Assert.Equal("Line one", string.Concat(lines[0].Select(r => r.Text)));
        Assert.Equal("Line two", string.Concat(lines[1].Select(r => r.Text)));
    }

    [Fact]
    public void WrapLines_EmptyRuns_ReturnsOneEmptyLine()
    {
        var lines = LineWrapper.WrapLines([], graphics, LayoutConstants.ContentWidth);

        Assert.Single(lines);
        Assert.Empty(lines[0]);
    }

    [Fact]
    public void SplitIntoWordSegments_SplitsOnSpaces()
    {
        var segments = LineWrapper.SplitIntoWordSegments("hello world foo");

        Assert.Equal(3, segments.Count);
        Assert.Equal("hello ", segments[0]);
        Assert.Equal("world ", segments[1]);
        Assert.Equal("foo", segments[2]);
    }

    [Fact]
    public void SplitIntoWordSegments_SingleWord_ReturnsSingleSegment()
    {
        var segments = LineWrapper.SplitIntoWordSegments("hello");

        Assert.Single(segments);
        Assert.Equal("hello", segments[0]);
    }

    [Fact]
    public void WrapLines_WithHardBreakTracking_ReportsBreakLines()
    {
        var font = new XFont(LayoutConstants.BodyFontFamily, LayoutConstants.BodyFontSize);
        var runs = new List<TextRun>
        {
            new("Line one", font),
            new("", font, IsLineBreak: true),
            new("Line two", font)
        };

        var lines = LineWrapper.WrapLines(runs, graphics, LayoutConstants.ContentWidth, out var hardBreakLines);

        Assert.Equal(2, lines.Count);
        Assert.Contains(0, hardBreakLines);
        Assert.DoesNotContain(1, hardBreakLines);
    }

    [Fact]
    public void WrapLines_WordWrapOnly_NoHardBreakLines()
    {
        var font = new XFont(LayoutConstants.BodyFontFamily, LayoutConstants.BodyFontSize);
        var longText = string.Join(" ", Enumerable.Repeat("word", 80));
        var runs = new List<TextRun> { new(longText, font) };

        var lines = LineWrapper.WrapLines(runs, graphics, LayoutConstants.ContentWidth, out var hardBreakLines);

        Assert.True(lines.Count > 1);
        Assert.Empty(hardBreakLines);
    }

    [Fact]
    public void WrapLinesPreferCommaBreak_ShortText_ReturnsSingleLine()
    {
        var font = new XFont(LayoutConstants.BodyFontFamily, LayoutConstants.BodyFontSize);
        var runs = new List<TextRun> { new("A, B, C", font) };

        var lines = LineWrapper.WrapLinesPreferCommaBreak(runs, graphics, LayoutConstants.ContentWidth);

        Assert.Single(lines);
        Assert.Equal("A, B, C", string.Concat(lines[0].Select(r => r.Text)));
    }

    [Fact]
    public void WrapLinesPreferCommaBreak_LongCommaSeparated_BreaksOnComma()
    {
        var font = new XFont(LayoutConstants.BodyFontFamily, LayoutConstants.BodyFontSize);
        var clauses = Enumerable.Range(1, 20).Select(i => $"clause number {i}");
        var text = string.Join(", ", clauses);
        var runs = new List<TextRun> { new(text, font) };

        var lines = LineWrapper.WrapLinesPreferCommaBreak(runs, graphics, LayoutConstants.ContentWidth);

        Assert.True(lines.Count > 1);
        // First line should end with a comma-delimited clause (trimmed)
        var firstLineText = string.Concat(lines[0].Select(r => r.Text));
        Assert.Contains(",", firstLineText);
    }

    [Fact]
    public void WrapLinesPreferCommaBreak_NoCommas_FallsBackToWordWrap()
    {
        var font = new XFont(LayoutConstants.BodyFontFamily, LayoutConstants.BodyFontSize);
        var longText = string.Join(" ", Enumerable.Repeat("word", 80));
        var runs = new List<TextRun> { new(longText, font) };

        var lines = LineWrapper.WrapLinesPreferCommaBreak(runs, graphics, LayoutConstants.ContentWidth);

        Assert.True(lines.Count > 1);
    }

    [Fact]
    public void WrapLinesPreferCommaBreak_EmptyRuns_ReturnsOneEmptyLine()
    {
        var lines = LineWrapper.WrapLinesPreferCommaBreak([], graphics, LayoutConstants.ContentWidth);

        Assert.Single(lines);
        Assert.Empty(lines[0]);
    }
}
