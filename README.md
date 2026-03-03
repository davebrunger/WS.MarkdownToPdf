# WS.MarkdownToPdf

A .NET 10 library that converts Markdown documents to PDF using [Markdig](https://github.com/xoofx/markdig) for parsing and [PDFsharp](https://www.pdfsharp.net/) for rendering.

## Features

### Supported Markdown Elements

| Element | Syntax | Notes |
|---|---|---|
| Headings | `# H1` through `###### H6` | Scaled font sizes (20 → 8 pt) |
| Bold | `**text**` | |
| Italic | `*text*` | |
| Strikethrough | `~~text~~` | |
| Inline code | `` `code` `` | Rendered in Courier New |
| Paragraphs | Plain text | Body text in Times New Roman 12 pt |
| Bullet lists | `- item` | Single-level |
| Ordered lists | `1. item` | Single-level |
| Block quotes | `> text` | Grey left bar, indented, single-level |
| Tables | Pipe tables | Equal-width columns, bold header row |
| Thematic breaks | `---` | Horizontal rule |
| Multi-column layout | `<!-- columns: N -->` | 2+ columns via HTML comments |

### PDF Output

- **A4 page size** (210 × 297 mm) with 20 mm margins
- **Automatic pagination** with orphan-heading prevention
- **Cross-platform font resolution** — Windows fonts natively; system TTF fonts on Linux/macOS

## Quick Start

### Installation

Add a project reference (not yet published as a NuGet package):

```xml
<ProjectReference Include="path\to\src\WS.MarkdownToPdf\WS.MarkdownToPdf.csproj" />
```

### Usage

```csharp
using WS.MarkdownToPdf;

var converter = new MarkdownToPdfConverter();

// Markdown string → PDF file
converter.ConvertFile("input.md", "output.pdf");

// Markdown string → byte array
byte[] pdf = converter.ConvertToBytes("# Hello\n\nWorld");

// Markdown string → Stream
using var stream = new MemoryStream();
converter.ConvertToStream("# Hello\n\nWorld", stream);
```

## Usage Guide

### Basic Conversion

The library exposes a single entry point — `MarkdownToPdfConverter` — with three conversion methods:

```csharp
using WS.MarkdownToPdf;

var converter = new MarkdownToPdfConverter();
```

#### File to File

Read a `.md` file and write the PDF directly to disk:

```csharp
converter.ConvertFile("report.md", "report.pdf");
```

#### String to Byte Array

Useful when you need to return PDF content from an API or store it in a database:

```csharp
string markdown = "# Invoice\n\nAmount: **$42.00**";
byte[] pdf = converter.ConvertToBytes(markdown);

// e.g. return from an ASP.NET controller
return File(pdf, "application/pdf", "invoice.pdf");
```

#### String to Stream

Write directly to any writable `Stream` — a `FileStream`, `MemoryStream`, HTTP response body, etc.:

```csharp
using var stream = new MemoryStream();
converter.ConvertToStream("# Hello\n\nWorld", stream);
```

### Supported Markdown Syntax

Below is a quick reference of what you can include in your Markdown and how it appears in the PDF.

**Text formatting**

```markdown
Regular text, **bold**, *italic*, ~~strikethrough~~, and `inline code`.
```

**Headings** (levels 1–6, rendered at 24 → 10 pt)

```markdown
# Heading 1
## Heading 2
### Heading 3
```

**Lists** (single-level only)

```markdown
- Bullet item a
- Bullet item b

1. Numbered item
2. Numbered item
```

**Block quotes** (single-level)

```markdown
> Important note displayed with a grey bar.
```

**Tables** (pipe-table syntax, equal-width columns)

```markdown
| Name  | Value |
|-------|-------|
| Alpha | 1     |
| Beta  | 2     |
```

**Thematic breaks**

```markdown
---
```

**Multi-column layout** (2 or more columns, invisible to other renderers)

```markdown
<!-- columns: 2 -->

First paragraph goes in the left column.

Second paragraph goes in the right column.

Third paragraph goes in whichever column needs it.

<!-- /columns -->
```

Three columns:

```markdown
<!-- columns: 3 -->

Block one.

Block two.

Block three.

Block four.

Block five.

Block six.

<!-- /columns -->
```

- Use `<!-- columns: N -->` to start an N-column section (N ≥ 2)
- Use `<!-- /columns -->` to end the section
- Content is distributed automatically — blocks flow left-to-right, balancing column heights when everything fits on the page
- When content exceeds the remaining page space, columns are filled to the bottom of the page and remaining blocks overflow to the next page (still in column layout)
- All standard block elements (paragraphs, headings, lists, tables, quotes, thematic breaks) work inside columns
- The directives are standard HTML comments, so other Markdown renderers (GitHub, VS Code preview, etc.) silently ignore them

### Error Handling

The converter throws `UnsupportedMarkdownException` when it encounters a Markdown element that has no renderer — for example fenced code blocks, images, or nested lists. Catch it to provide a user-friendly message:

```csharp
using WS.MarkdownToPdf;
using WS.MarkdownToPdf.Exceptions;

try
{
    converter.ConvertFile("input.md", "output.pdf");
}
catch (UnsupportedMarkdownException ex)
{
    Console.Error.WriteLine($"Cannot convert: {ex.Message}");
}
```

`ArgumentNullException` is thrown if `null` is passed for any required parameter.

### PDF Layout Defaults

| Property | Value |
|---|---|
| Page size | A4 (210 × 297 mm) |
| Margins | 20 mm on all sides |
| Body font | Times New Roman 10 pt |
| Mono font | Courier New (inline code) |
| Line spacing | 1.2× |
| Paragraph spacing | 5 pt |

Headings are automatically prevented from appearing as orphans at the bottom of a page — if a heading won't fit with at least some following content, it is moved to the next page.

### Cross-Platform Font Resolution

- **Windows** — uses native system fonts.
- **Linux / macOS** — resolves TTF fonts from standard system font directories via `SystemFontResolver`.

No additional font installation is required on typical desktop or CI environments.

## Command-Line Tool

A CLI tool (`SampleGenerator`) is included for quick Markdown-to-PDF conversion without writing any code.

### Running

```powershell
dotnet run --project tools/SampleGenerator -- <input> <output>
```

| Argument | Position | Default | Description |
|---|---|---|---|
| `input` | 1st | `sample.md` | Path to the Markdown file to convert |
| `output` | 2nd | `sample.pdf` | Path for the generated PDF |

Both arguments are positional and optional.

### Examples

Convert a specific file:

```powershell
dotnet run --project tools/SampleGenerator -- docs/report.md docs/report.pdf
```

Use defaults (reads `sample.md`, writes `sample.pdf`):

```powershell
dotnet run --project tools/SampleGenerator
```

Convert with only an input path (output defaults to `sample.pdf`):

```powershell
dotnet run --project tools/SampleGenerator -- meeting-notes.md
```

On success the tool prints the absolute path of the generated PDF:

```
PDF generated: C:\Users\you\project\docs\report.pdf
```

### Building a Standalone Executable

You can publish the tool as a self-contained executable so it can be run without `dotnet run`:

```powershell
dotnet publish tools/SampleGenerator -c Release -o ./publish
./publish/SampleGenerator report.md report.pdf
```

## Architecture

```
MarkdownToPdfConverter          # Public API entry point
├── IMarkdownParser             # Abstraction over Markdown parsing
│   └── MarkdigParser           # Markdig implementation (pipe tables + strikethrough)
└── IPdfRenderer                # Abstraction over PDF rendering
    └── PdfRenderer             # AST walker dispatching to block renderers
        ├── HeadingRenderer
        ├── ParagraphRenderer
        ├── ListRenderer
        ├── QuoteBlockRenderer
        ├── TableRenderer
        ├── ThematicBreakRenderer
        └── InlineRenderer      # Resolves bold/italic/strikethrough/code runs
```

Layout constants (page size, margins, fonts, spacing) are centralised in `LayoutConstants`. Font initialisation is handled by `FontSetup`, which delegates to `SystemFontResolver` on non-Windows platforms.

Unsupported Markdown elements (e.g. fenced code blocks, images, nested lists) throw `UnsupportedMarkdownException` at render time.

## Building & Testing

```powershell
# Build
dotnet build

# Run tests
dotnet test
```

Tests use **xUnit** and cover each block renderer, inline formatting, pagination, and end-to-end conversion.

## License

[MIT](LICENSE) — Copyright © 2026 David Brunger
