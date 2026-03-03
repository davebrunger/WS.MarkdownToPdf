using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using PdfSharp.Drawing;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Parsing;
using WS.MarkdownToPdf.Rendering.InlineRenderers;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class InlineRendererTests
{
    private readonly InlineRenderer sut;
    private readonly MarkdigParser parser = new();

    public InlineRendererTests()
    {
        // Ensure fonts are initialized
        FontSetup.EnsureInitialized();
        sut = new InlineRenderer();
    }

    private ContainerInline GetInlines(string markdown)
    {
        var doc = parser.Parse(markdown);
        var paragraph = doc.Descendants<ParagraphBlock>().First();
        return paragraph.Inline!;
    }

    [Fact]
    public void GetTextRuns_PlainText_ReturnsSingleRegularRun()
    {
        var runs = sut.GetTextRuns(GetInlines("Hello world"), LayoutConstants.BodyFontSize);

        Assert.Single(runs);
        Assert.Equal("Hello world", runs[0].Text);
        Assert.Equal(XFontStyleEx.Regular, runs[0].Font.Style);
        Assert.False(runs[0].IsStrikethrough);
    }

    [Fact]
    public void GetTextRuns_BoldText_ReturnsBoldRun()
    {
        var runs = sut.GetTextRuns(GetInlines("**bold**"), LayoutConstants.BodyFontSize);

        var boldRun = runs.Single(r => r.Text == "bold");
        Assert.Equal(XFontStyleEx.Bold, boldRun.Font.Style);
    }

    [Fact]
    public void GetTextRuns_ItalicText_ReturnsItalicRun()
    {
        var runs = sut.GetTextRuns(GetInlines("*italic*"), LayoutConstants.BodyFontSize);

        var italicRun = runs.Single(r => r.Text == "italic");
        Assert.Equal(XFontStyleEx.Italic, italicRun.Font.Style);
    }

    [Fact]
    public void GetTextRuns_BoldAndItalic_ReturnsMixedRuns()
    {
        var runs = sut.GetTextRuns(GetInlines("**bold** and *italic*"), LayoutConstants.BodyFontSize);

        var boldRun = runs.Single(r => r.Text == "bold");
        Assert.Equal(XFontStyleEx.Bold, boldRun.Font.Style);

        var italicRun = runs.Single(r => r.Text == "italic");
        Assert.Equal(XFontStyleEx.Italic, italicRun.Font.Style);

        var plainRun = runs.Single(r => r.Text == " and ");
        Assert.Equal(XFontStyleEx.Regular, plainRun.Font.Style);
    }

    [Fact]
    public void GetTextRuns_Strikethrough_ReturnsStrikethroughRun()
    {
        var runs = sut.GetTextRuns(GetInlines("~~struck~~"), LayoutConstants.BodyFontSize);

        var struckRun = runs.Single(r => r.Text == "struck");
        Assert.True(struckRun.IsStrikethrough);
    }

    [Fact]
    public void GetTextRuns_InlineCode_ReturnsMonoFontRun()
    {
        var runs = sut.GetTextRuns(GetInlines("`code`"), LayoutConstants.BodyFontSize);

        var codeRun = runs.Single(r => r.Text == "code");
        Assert.Contains("Courier", codeRun.Font.FontFamily.Name);
    }

    [Fact]
    public void GetTextRuns_BoldItalic_ReturnsBoldItalicRun()
    {
        var runs = sut.GetTextRuns(GetInlines("***bold italic***"), LayoutConstants.BodyFontSize);

        var run = runs.Single(r => r.Text == "bold italic");
        Assert.True(run.Font.Bold);
        Assert.True(run.Font.Italic);
    }

    [Fact]
    public void GetTextRuns_SoftLineBreak_ProducesSoftLineBreakRun()
    {
        var doc = parser.Parse("> Line one\n> Line two");
        var paragraph = doc.Descendants<ParagraphBlock>().First();

        var runs = sut.GetTextRuns(paragraph.Inline!, LayoutConstants.BodyFontSize);

        Assert.Contains(runs, r => r.IsSoftLineBreak);
        var textRuns = runs.Where(r => !r.IsSoftLineBreak).ToList();
        Assert.Contains(textRuns, r => r.Text.Contains("Line one"));
        Assert.Contains(textRuns, r => r.Text.Contains("Line two"));
    }
}
