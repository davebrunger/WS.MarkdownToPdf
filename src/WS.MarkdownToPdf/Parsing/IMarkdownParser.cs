using Markdig.Syntax;

namespace WS.MarkdownToPdf.Parsing;

/// <summary>
/// Parses Markdown text into a Markdig AST.
/// </summary>
public interface IMarkdownParser
{
    /// <summary>
    /// Parses the given Markdown string and returns a <see cref="MarkdownDocument"/>.
    /// </summary>
    MarkdownDocument Parse(string markdown);
}
