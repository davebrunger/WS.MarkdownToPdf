---
name: markdig
description: Guidance for parsing Markdown in .NET using the Markdig library — pipeline configuration, extensions, AST walking, custom renderers, HTML output, and testing patterns.
---

# Markdown Parsing with Markdig

## Library

Use **Markdig** — a fast, CommonMark-compliant, extensible Markdown processor for .NET.

```shell
dotnet add package Markdig
```

- **License**: BSD-2-Clause
- **Docs**: <https://xoofx.github.io/markdig>
- **Source**: <https://github.com/xoofx/markdig>

---

## Pipeline Setup

Always build a `MarkdownPipeline` — even if using defaults — to make extension choices explicit:

```csharp
using Markdig;

var pipeline = new MarkdownPipelineBuilder()
    .UseAdvancedExtensions()
    .Build();

string html = Markdown.ToHtml(markdownText, pipeline);
```

- `UseAdvancedExtensions()` enables most built-in extensions (tables, footnotes, task lists, diagrams, math, auto-links, etc.) **except** Emoji, SoftLine-as-HardLine, Bootstrap, YAML Front Matter, JiraLinks, and SmartyPants
- For full control, enable extensions individually (see below)
- Build the pipeline once and reuse it — `MarkdownPipeline` is immutable and thread-safe

---

## Choosing Extensions

Enable only what you need. Common extensions:

```csharp
var pipeline = new MarkdownPipelineBuilder()
    .UsePipeTables()
    .UseAutoLinks()
    .UseTaskLists()
    .UseFootnotes()
    .UseAutoIdentifiers()
    .UseYamlFrontMatter()
    .UseEmphasisExtras()       // strikethrough, subscript, superscript
    .UseMathematics()          // $inline$ and $$block$$ LaTeX
    .UseDiagrams()             // mermaid, nomnoml
    .UseFigures()
    .Build();
```

| Extension method | Adds support for |
|---|---|
| `UsePipeTables()` | GitHub-style pipe tables |
| `UseGridTables()` | Pandoc-style grid tables |
| `UseAutoLinks()` | Auto-detect URLs |
| `UseTaskLists()` | `- [x]` checkboxes |
| `UseFootnotes()` | `[^1]` footnotes |
| `UseAutoIdentifiers()` | Heading anchors |
| `UseYamlFrontMatter()` | Strip YAML front matter from output |
| `UseEmphasisExtras()` | `~~strike~~`, `~sub~`, `^sup^`, `++ins++`, `==mark==` |
| `UseMathematics()` | LaTeX math blocks |
| `UseDiagrams()` | Mermaid / nomnoml code blocks |
| `UseDefinitionLists()` | Definition lists |
| `UseCustomContainers()` | `:::` div containers |
| `UseSmartyPants()` | Smart quotes and dashes |
| `UseEmojiAndSmiley()` | `:emoji:` shortcodes |

---

## Converting Markdown to HTML

**Simple conversion:**

```csharp
string html = Markdown.ToHtml(markdownText, pipeline);
```

**With a `TextWriter` for streaming large documents:**

```csharp
using var writer = new StringWriter();
var renderer = new HtmlRenderer(writer);
pipeline.Setup(renderer);

var document = Markdown.Parse(markdownText, pipeline);
renderer.Render(document);
writer.Flush();

string html = writer.ToString();
```

---

## Working with the AST

Parse to a `MarkdownDocument` to inspect or transform the syntax tree:

```csharp
var document = Markdown.Parse(markdownText, pipeline);

foreach (var node in document.Descendants())
{
    if (node is HeadingBlock heading)
    {
        // heading.Level, heading.Inline, heading.Line, heading.Column
    }

    if (node is ParagraphBlock paragraph)
    {
        // paragraph.Inline contains inline elements
    }
}
```

### Key AST types

| Block types | Inline types |
|---|---|
| `HeadingBlock` | `LiteralInline` |
| `ParagraphBlock` | `EmphasisInline` |
| `ListBlock` / `ListItemBlock` | `LinkInline` |
| `QuoteBlock` | `CodeInline` |
| `FencedCodeBlock` | `LineBreakInline` |
| `ThematicBreakBlock` | `HtmlInline` |
| `HtmlBlock` | `AutolinkInline` |
| `Table` / `TableRow` / `TableCell` | |

- Every node has `Line` and `Column` properties for source location
- Use `node.Descendants()` or `node.Descendants<T>()` for type-filtered traversal

---

## Extracting Plain Text

Use `Markdown.ToPlainText` to strip all formatting:

```csharp
string plainText = Markdown.ToPlainText(markdownText, pipeline);
```

---

## Custom Renderers

Implement a custom `IMarkdownObjectRenderer` to control output for specific node types:

```csharp
public class CustomHeadingRenderer : HtmlObjectRenderer<HeadingBlock>
{
    protected override void Write(HtmlRenderer renderer, HeadingBlock heading)
    {
        renderer.Write($"<h{heading.Level} class=\"custom\">");
        renderer.WriteLeafInline(heading);
        renderer.Write($"</h{heading.Level}>");
    }
}

// Register it
var renderer = new HtmlRenderer(writer);
renderer.ObjectRenderers.ReplaceOrAdd<HtmlObjectRenderer<HeadingBlock>>(new CustomHeadingRenderer());
```

---

## Roundtrip / Lossless Parsing

Use `EnableTrackTrivia()` to preserve whitespace and formatting for lossless parse-render cycles:

```csharp
var pipeline = new MarkdownPipelineBuilder()
    .EnableTrackTrivia()
    .Build();

var document = Markdown.Parse(markdownText, pipeline);
// Modify the AST...
string output = document.ToMarkdownString();
```

---

## YAML Front Matter

To parse YAML front matter alongside the document:

```csharp
var pipeline = new MarkdownPipelineBuilder()
    .UseYamlFrontMatter()
    .Build();

var document = Markdown.Parse(markdownText, pipeline);
var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();

if (yamlBlock is not null)
{
    var yaml = markdownText.Substring(
        yamlBlock.Span.Start,
        yamlBlock.Span.Length);
}
```

The `UseYamlFrontMatter()` extension strips the front matter from HTML output automatically.

---

## Design Guidance

- **Inject the pipeline, not the library** — define an `IMarkdownParser` abstraction in your domain; implement it with Markdig in the infrastructure layer
- **Build pipelines once** — they are immutable and thread-safe after construction
- **Keep rendering separate from parsing** — parse to AST first, then pass to a renderer. This enables swapping output formats (HTML, PDF, plain text)
- **Avoid string manipulation on HTML output** — transform the AST instead
- **Handle untrusted input** — Markdig does not sanitise HTML by default. Use `DisableHtml()` or a post-processing sanitiser for user-generated content

---

## Testing

```csharp
public class MarkdownParserTests
{
    private readonly MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseAutoIdentifiers()
        .Build();

    [Fact]
    public void ToHtml_WithHeading_ProducesH1Tag()
    {
        var html = Markdown.ToHtml("# Hello", pipeline);

        Assert.Contains("<h1", html);
        Assert.Contains("Hello", html);
    }

    [Theory]
    [InlineData("**bold**", "<strong>bold</strong>")]
    [InlineData("*italic*", "<em>italic</em>")]
    [InlineData("`code`", "<code>code</code>")]
    public void ToHtml_WithInlineFormatting_ProducesExpectedHtml(string markdown, string expected)
    {
        var html = Markdown.ToHtml(markdown, pipeline);

        Assert.Contains(expected, html);
    }

    [Fact]
    public void Parse_WithTable_ProducesTableBlock()
    {
        var markdown = "| A | B |\n|---|---|\n| 1 | 2 |";
        var document = Markdown.Parse(markdown, pipeline);

        Assert.Contains(document.Descendants<Table>(), _ => true);
    }
}
```

---

## References

- Documentation: <https://xoofx.github.io/markdig>
- Extension specs: <https://github.com/xoofx/markdig/blob/main/src/Markdig.Tests/Specs/readme.md>
- CommonMark spec: <https://spec.commonmark.org/>
