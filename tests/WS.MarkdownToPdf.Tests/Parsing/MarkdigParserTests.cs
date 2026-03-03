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
}
