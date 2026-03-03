using Markdig;
using Markdig.Syntax;

namespace WS.MarkdownToPdf.Parsing;

/// <summary>
/// Parses Markdown text into a Markdig AST using a constrained pipeline.
/// </summary>
public class MarkdigParser : IMarkdownParser
{
    private readonly MarkdownPipeline pipeline;

    public MarkdigParser()
    {
        pipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseEmphasisExtras(Markdig.Extensions.EmphasisExtras.EmphasisExtraOptions.Strikethrough)
            .Build();
    }

    /// <inheritdoc />
    public MarkdownDocument Parse(string markdown) =>
        Markdown.Parse(markdown, pipeline);
}
