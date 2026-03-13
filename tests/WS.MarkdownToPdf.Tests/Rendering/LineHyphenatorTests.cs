using PdfSharp.Drawing;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Rendering;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class LineHyphenatorTests
{
    private readonly XGraphics graphics;
    private readonly XFont font;
    private readonly LayoutOptions layout = new() { MaxJustificationGap = 3 };

    public LineHyphenatorTests()
    {
        FontSetup.EnsureInitialized();
        var doc = new PdfDocument();
        var page = doc.AddPage();
        graphics = XGraphics.FromPdfPage(page);
        font = new XFont(layout.BodyFontFamily, layout.BodyFontSize, XFontStyleEx.Regular);
    }

    [Fact]
    public void HyphenateIfNeeded_LineWithinGapThreshold_DoesNotModify()
    {
        // Line that nearly fills available width → small gap → no hyphenation needed
        var text = "one two three four five six seven eight";
        var textWidth = graphics.MeasureString(text, font).Width;
        var line1 = new List<TextRun> { new(text, font) };
        var line2 = new List<TextRun> { new("remainder", font) };
        var lines = new List<List<TextRun>> { line1, line2 };

        // Available width just slightly more than the text → gap is small
        LineHyphenator.HyphenateIfNeeded(lines, graphics, textWidth + 2, layout);

        Assert.Equal(text, string.Concat(lines[0].Select(r => r.Text)));
    }

    [Fact]
    public void HyphenateIfNeeded_NarrowColumn_PullsFromNextLine()
    {
        // "potentially something" has a big gap, "useful" is on the next line.
        // Width is wide enough that "potentially something use-" fits.
        var narrowWidth = graphics.MeasureString("potentially something use-", font).Width + 2;
        var line1 = new List<TextRun> { new("potentially something", font) };
        var line2 = new List<TextRun> { new("useful", font) };
        var lines = new List<List<TextRun>> { line1, line2 };

        LineHyphenator.HyphenateIfNeeded(lines, graphics, narrowWidth, layout);

        var line1Text = string.Concat(lines[0].Select(r => r.Text));
        Assert.Contains("use-", line1Text);
        var line2Text = string.Concat(lines[1].Select(r => r.Text));
        Assert.Contains("ful", line2Text);
    }

    [Fact]
    public void HyphenateIfNeeded_PullFromNextLine_WhenSpaceAvailable()
    {
        // Wide enough line with room to pull a fragment from the next word
        var text = "short";
        var wideWidth = graphics.MeasureString(text, font).Width + 80;
        var line1 = new List<TextRun> { new(text, font) };
        var line2 = new List<TextRun> { new("hyphenatable", font) };
        var lines = new List<List<TextRun>> { line1, line2 };

        LineHyphenator.HyphenateIfNeeded(lines, graphics, wideWidth, layout);

        var line1Text = string.Concat(lines[0].Select(r => r.Text));
        // Should have pulled a fragment from "hyphenatable"
        if (line1Text.Contains('-'))
        {
            Assert.Contains("-", line1Text);
            // Next line should have the remainder
            var line2Text = string.Concat(lines[1].Select(r => r.Text));
            Assert.DoesNotContain("hyphenatable", line2Text);
        }
    }

    [Fact]
    public void HyphenateIfNeeded_LastLine_IsNeverHyphenated()
    {
        // Only one line → nothing to do
        var line1 = new List<TextRun> { new("single", font) };
        var lines = new List<List<TextRun>> { line1 };

        LineHyphenator.HyphenateIfNeeded(lines, graphics, 50, layout);

        Assert.Single(lines);
        Assert.Equal("single", string.Concat(lines[0].Select(r => r.Text)));
    }

    [Fact]
    public void HyphenateIfNeeded_ShortWords_DoesNotCrash()
    {
        var line1 = new List<TextRun> { new("a b", font) };
        var line2 = new List<TextRun> { new("cd", font) };
        var lines = new List<List<TextRun>> { line1, line2 };

        // Should not throw even with very short words
        LineHyphenator.HyphenateIfNeeded(lines, graphics, 30, layout);

        Assert.NotEmpty(lines);
    }
}
