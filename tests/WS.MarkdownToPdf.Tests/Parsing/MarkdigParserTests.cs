using Markdig.Extensions.Tables;
using Markdig.Syntax;
using WS.MarkdownToPdf.Parsing;

namespace WS.MarkdownToPdf.Tests.Parsing;

public class MarkdigParserTests
{
    private readonly MarkdigParser parser = new();

    [Fact]
    public void Parse_Heading_ReturnsAstWithHeadingBlock()
    {
        var document = parser.Parse("# Hello");

        var heading = Assert.Single(document.OfType<HeadingBlock>());
        Assert.Equal(1, heading.Level);
    }

    [Fact]
    public void Parse_Table_ReturnsAstWithTable()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";

        var document = parser.Parse(markdown);

        Assert.Contains(document, block => block is Table);
    }

    [Fact]
    public void Parse_Paragraph_ReturnsParagraphBlock()
    {
        var document = parser.Parse("Just some text");

        Assert.Single(document.OfType<ParagraphBlock>());
    }

    [Fact]
    public void Parse_BoldEmphasis_ContainsEmphasisInline()
    {
        var document = parser.Parse("**bold text**");

        var paragraph = Assert.Single(document.OfType<ParagraphBlock>());
        Assert.NotNull(paragraph.Inline);
        var emphasis = paragraph.Inline!.Descendants<Markdig.Syntax.Inlines.EmphasisInline>().FirstOrDefault();
        Assert.NotNull(emphasis);
        Assert.Equal(2, emphasis!.DelimiterCount); // ** = bold
    }

    [Fact]
    public void Parse_ItalicEmphasis_ContainsEmphasisInline()
    {
        var document = parser.Parse("*italic text*");

        var paragraph = Assert.Single(document.OfType<ParagraphBlock>());
        var emphasis = paragraph.Inline!.Descendants<Markdig.Syntax.Inlines.EmphasisInline>().FirstOrDefault();
        Assert.NotNull(emphasis);
        Assert.Equal(1, emphasis!.DelimiterCount); // * = italic
    }

    [Fact]
    public void Parse_Strikethrough_ContainsEmphasisWithTilde()
    {
        var document = parser.Parse("~~struck~~");

        var paragraph = Assert.Single(document.OfType<ParagraphBlock>());
        var emphasis = paragraph.Inline!.Descendants<Markdig.Syntax.Inlines.EmphasisInline>().FirstOrDefault();
        Assert.NotNull(emphasis);
        Assert.Equal('~', emphasis!.DelimiterChar);
    }

    [Fact]
    public void Parse_InlineCode_ContainsCodeInline()
    {
        var document = parser.Parse("`some code`");

        var paragraph = Assert.Single(document.OfType<ParagraphBlock>());
        var code = paragraph.Inline!.Descendants<Markdig.Syntax.Inlines.CodeInline>().FirstOrDefault();
        Assert.NotNull(code);
        Assert.Equal("some code", code!.Content);
    }

    [Fact]
    public void Parse_List_ReturnsListBlock()
    {
        var document = parser.Parse("- item one\n- item two");

        Assert.Single(document.OfType<ListBlock>());
    }

    [Fact]
    public void Parse_ThematicBreak_ReturnsThematicBreakBlock()
    {
        var document = parser.Parse("---");

        Assert.Single(document.OfType<ThematicBreakBlock>());
    }

    [Fact]
    public void Parse_BlockQuote_ReturnsQuoteBlock()
    {
        var document = parser.Parse("> quoted text");

        Assert.Single(document.OfType<QuoteBlock>());
    }
}
