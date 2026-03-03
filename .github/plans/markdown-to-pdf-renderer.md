# Markdown to PDF Renderer — Implementation Plan

## Overview

Build a direct Markdown-to-PDF renderer that parses Markdown using **Markdig** and renders to PDF using **PDFsharp** — no intermediate HTML step. The renderer supports a constrained subset of Markdown and throws on unrecognised syntax.

## Supported Markdown Features

| Feature | Markdown syntax | Rendering |
|---------|----------------|-----------|
| Headings (H1–H6) | `# … ######` | Bold, decreasing font size |
| Bold | `**text**` | Times New Roman Bold |
| Italic | `*text*` | Times New Roman Italic |
| Strikethrough | `~~text~~` | Times New Roman with strikethrough line |
| Horizontal rule | `---` | Thin line spanning page width |
| Bulleted list | `- item` | Bullet character + indented text (single level only) |
| Numbered list | `1. item` | Number + indented text (single level only) |
| Block quote | `> text` | Indented text with grey left bar (single level only) |
| Tables | Pipe table `\| A \| B \|` | Grid-drawn table with header row |

### Explicitly unsupported

- Nested lists, nested block quotes
- Images, links, footnotes, task lists, code blocks (fenced)
- HTML inline/blocks
- Any Markdig extension not listed above

**Unrecognised AST node types must throw an `UnsupportedMarkdownException`.**

---

## Typography & Layout Constants

| Property | Value |
|----------|-------|
| Page size | A4 (210 × 297 mm) |
| Margins | 2 cm all sides |
| Body font | Times New Roman, 12 pt |
| Heading fonts | Times New Roman Bold — H1: 24 pt, H2: 20 pt, H3: 16 pt, H4: 14 pt, H5: 12 pt, H6: 10 pt |
| Line spacing | 1.2 × font size |
| Paragraph spacing | 6 pt after each block |
| List indent | 20 pt from left margin |
| Block quote indent | 20 pt from left margin |
| Block quote bar | 2 pt wide, light grey, 4 pt left of text |
| Table cell padding | 4 pt |
| Horizontal rule | 0.5 pt black line, full content width |

---

## Solution Structure

```
WS.MardownToPdf.sln
├── src/
│   └── WS.MarkdownToPdf/                    # Main library
│       ├── WS.MarkdownToPdf.csproj
│       ├── Parsing/
│       │   ├── IMarkdownParser.cs            # Abstraction over Markdig
│       │   └── MarkdigParser.cs              # Wraps Markdig, returns MarkdownDocument
│       ├── Rendering/
│       │   ├── IPdfRenderer.cs               # Takes MarkdownDocument, returns PdfDocument
│       │   ├── PdfRenderer.cs                # Orchestrator — walks AST, delegates to renderers
│       │   ├── RenderContext.cs              # Tracks current Y position, page, fonts, margins
│       │   └── BlockRenderers/
│       │       ├── HeadingRenderer.cs
│       │       ├── ParagraphRenderer.cs
│       │       ├── ListRenderer.cs
│       │       ├── QuoteBlockRenderer.cs
│       │       ├── TableRenderer.cs
│       │       └── ThematicBreakRenderer.cs
│       ├── Rendering/
│       │   └── InlineRenderers/
│       │       ├── InlineRenderer.cs         # Walks inline AST nodes
│       │       └── TextRun.cs                # Groups text with a specific font/style
│       ├── Layout/
│       │   └── LayoutConstants.cs            # Page size, margins, font sizes, spacing
│       ├── Exceptions/
│       │   └── UnsupportedMarkdownException.cs
│       └── MarkdownToPdfConverter.cs         # Public entry point: file/string → PDF bytes/stream
│
└── tests/
    └── WS.MarkdownToPdf.Tests/               # xUnit test project
        ├── WS.MarkdownToPdf.Tests.csproj
        ├── Parsing/
        │   └── MarkdigParserTests.cs
        ├── Rendering/
        │   ├── PdfRendererTests.cs
        │   ├── HeadingRendererTests.cs
        │   ├── ParagraphRendererTests.cs
        │   ├── ListRendererTests.cs
        │   ├── QuoteBlockRendererTests.cs
        │   ├── TableRendererTests.cs
        │   └── ThematicBreakRendererTests.cs
        ├── InlineRendererTests.cs
        ├── UnsupportedMarkdownTests.cs
        └── ConverterIntegrationTests.cs
```

---

## Implementation Steps

Work in TDD (Red → Green → Refactor) throughout. Each step starts with a failing test.

### Phase 1 — Project scaffolding

1. Create `src/WS.MarkdownToPdf` class library targeting `net10.0`
2. Create `tests/WS.MarkdownToPdf.Tests` xUnit test project targeting `net10.0`
3. Add NuGet references: `Markdig` to library, `xunit` + `Microsoft.NET.Test.Sdk` to tests
4. Add NuGet reference: `PDFsharp` to library
5. Add both projects to `WS.MardownToPdf.sln`
6. Add `Directory.Build.props` with shared properties (nullable, warnings-as-errors)
7. Confirm `dotnet build` and `dotnet test` pass with zero tests

### Phase 2 — Markdown parsing

8. Define `IMarkdownParser` returning `Markdig.Syntax.MarkdownDocument`
9. Implement `MarkdigParser` — configure pipeline with pipe tables and emphasis extras (strikethrough) only
10. **Test**: parse heading → AST contains `HeadingBlock`
11. **Test**: parse table → AST contains `Table`

### Phase 3 — Rendering foundation

12. Create `LayoutConstants` with all typography/spacing values
13. Create `RenderContext` — tracks current Y position, current `XGraphics`, `PdfDocument`, current page, and exposes `AddPage()` to handle page breaks
14. Create `UnsupportedMarkdownException`
15. Create `IPdfRenderer` and `PdfRenderer` — walks top-level blocks, dispatches to block renderers
16. **Test**: render empty document → produces valid single-page PDF (non-zero byte stream)
17. **Test**: render document with unsupported node type → throws `UnsupportedMarkdownException`

### Phase 4 — Block renderers (one at a time, TDD)

Each renderer is tested independently using Markdig-parsed AST nodes.

18. **ParagraphRenderer** — renders a `ParagraphBlock` as plain text
    - Test: single paragraph → PDF contains text positioned below top margin
19. **HeadingRenderer** — renders `HeadingBlock` with level-appropriate font size
    - Test: `# Title` → drawn with 24 pt bold font
    - Test: `### Subtitle` → drawn with 16 pt bold font
20. **ThematicBreakRenderer** — renders `ThematicBreakBlock` as horizontal line
    - Test: `---` → draws a line spanning content width
21. **ListRenderer** — renders `ListBlock` (bulleted and ordered, single level)
    - Test: bulleted list with 3 items → 3 lines each prefixed with bullet
    - Test: numbered list with 3 items → 3 lines prefixed with `1.`, `2.`, `3.`
    - Test: nested list → throws `UnsupportedMarkdownException`
22. **QuoteBlockRenderer** — renders `QuoteBlock` with indent and left bar
    - Test: `> quote text` → text indented with grey bar
    - Test: nested quote → throws `UnsupportedMarkdownException`
23. **TableRenderer** — renders `Table` as grid with header row
    - Test: 2×2 table → draws grid lines and cell text
    - Test: table with header → header row text is bold

### Phase 5 — Inline rendering

24. **InlineRenderer** — walks `ContainerInline` children and builds styled text runs
    - `LiteralInline` → regular font
    - `EmphasisInline` (single `*`) → italic font
    - `EmphasisInline` (double `**`) → bold font
    - `EmphasisInline` (`~~`) → strikethrough (regular font + line through text)
    - Any other inline type → throws `UnsupportedMarkdownException`
25. **Test**: `**bold** and *italic*` → two text runs with correct fonts
27. **Test**: `~~struck~~` → text run with strikethrough
28. Wire inline renderer into ParagraphRenderer, HeadingRenderer, ListRenderer, QuoteBlockRenderer, and TableRenderer

### Phase 6 — Automatic page breaks

Page breaks are calculated automatically — the user never specifies them. Each block renderer must measure its height before drawing. The `RenderContext` handles page overflow.

**Rules:**
- **Paragraphs are never split across pages.** If a paragraph does not fit on the current page, move it entirely to the next page.
- **Headings stay with the following block.** When rendering a heading, measure the heading plus the next block together. If both do not fit on the remaining page, move the heading (and the following block) to a new page.
- Lists, block quotes, and tables follow the same paragraph rule — if the block does not fit, move it to the next page. (A very tall table or list that exceeds a full page height is an edge case deferred for now.)

29. Add `MeasureHeight()` to each block renderer so it can report its height without drawing
30. In `PdfRenderer`, before rendering a heading, peek at the next block and check combined height; page-break if needed
31. Before rendering any non-heading block, check its measured height against remaining space; page-break if needed
32. **Test**: paragraph that would overflow the page → moved entirely to the next page (not split)
33. **Test**: heading at the bottom of a page followed by a paragraph → both appear on the next page
34. **Test**: long document with many paragraphs → produces multiple pages with no split paragraphs

### Phase 7 — Public API & integration

35. Implement `MarkdownToPdfConverter` as the public entry point
    - `ConvertToStream(string markdown, Stream output)`
    - `ConvertToBytes(string markdown) → byte[]`
    - `ConvertFile(string inputPath, string outputPath)`
36. **Integration test**: full Markdown document with all supported features → produces valid multi-page PDF
37. **Integration test**: Markdown with unsupported feature (e.g. image) → throws `UnsupportedMarkdownException`

---

## Dependencies

| Package | Project | Purpose |
|---------|---------|---------|
| `Markdig` | WS.MarkdownToPdf | Markdown parsing to AST |
| `PDFsharp` | WS.MarkdownToPdf | PDF document creation and drawing |
| `xunit` | WS.MarkdownToPdf.Tests | Test framework |
| `xunit.runner.visualstudio` | WS.MarkdownToPdf.Tests | Test discovery |
| `Microsoft.NET.Test.Sdk` | WS.MarkdownToPdf.Tests | Test host |

---

## Key Design Decisions

1. **No HTML intermediate** — walk the Markdig AST directly and draw via PDFsharp's `XGraphics`
2. **Fail loudly** — unsupported Markdown throws `UnsupportedMarkdownException` rather than silently dropping content
3. **Single-level only** — nested lists/quotes throw; simplifies layout logic
4. **No customisation yet** — all layout values are constants, not configurable; a future `RenderOptions` class can replace `LayoutConstants`
5. **Stream-first output** — the converter writes to `Stream` for testability; file-based helpers wrap this
6. **Block renderers are stateless** — they receive `RenderContext` and return the updated Y position; this keeps them testable in isolation
7. **Automatic page breaks** — pages break automatically; paragraphs are never split across pages; headings are always kept on the same page as the block that follows them
