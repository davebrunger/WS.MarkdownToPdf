---
name: table-rendering
description: Rules for rendering Markdown pipe tables to PDF — progressive column-width fitting, text wrapping, cell alignment, comma-preferred line breaking, and clip rectangles. Use when modifying table layout, column sizing, or cell rendering in the TableRenderer.
---

# Table Rendering

Guidelines for rendering Markdig `Table` nodes to PDF using PDFsharp, covering column sizing, text wrapping, alignment, and line breaking.

## Column Width Algorithm (Progressive Wrapping)

Fit columns into the available page/column width using a four-step progressive strategy:

1. **Natural widths** — measure each column's unwrapped content width (max of header and data). If the total fits, use these widths directly.
2. **Wrap headers first** — for headers wider than their data column, progressively wrap onto 2 lines, then 3, then 4, etc. Re-check fit after each pass.
3. **Wrap data columns** — wrap the widest data column onto 2 lines (widest first), then the next widest, repeating at 3, 4, … lines until the table fits.
4. **Throw** — if the table still cannot fit after exhausting wrapping, throw `UnsupportedMarkdownException`.

- Use binary search (`FindWidthForLineCount`) to find the minimum column width that produces at most N wrapped lines for a given cell's content.
- Always measure natural widths from **data rows only** when deciding which columns are "wide"; header widths are handled in step 2.

## Alignment Rules

### Horizontal

- **First column** — right-aligned
- **Last column** — left-aligned
- **Middle columns** — centre-aligned

### Vertical

- **Header cells** — bottom-aligned within the row height
- **Data cells** — middle-aligned (vertically centred) within the row height

## Line Breaking

- **Tables** use `LineWrapper.WrapLinesPreferCommaBreak` — a two-pass approach that first breaks on `", "` boundaries (keeping clauses atomic), then re-wraps any overflowing lines on spaces.
- **All other text** (paragraphs, quotes, list items) uses the standard `LineWrapper.WrapLines` which breaks on spaces only.
- Do **not** modify `SplitIntoWordSegments` to add comma logic — keep the comma path isolated to `WrapLinesPreferCommaBreak`.

## Cell Rendering

- Apply `XGraphics.IntersectClip` with the cell rectangle before drawing text — this prevents content from bleeding into adjacent cells.
- Restore the graphics state after each cell.
- Row heights are computed dynamically by measuring wrapped content height for every cell in the row, then taking the maximum.

## Key Files

- `src/WS.MarkdownToPdf/Rendering/BlockRenderers/TableRenderer.cs` — main table block renderer (`FitColumnWidths`, `FindWidthForLineCount`, `RenderWrappedCell`, `ComputeHorizontalX`, `ComputeRowHeights`)
- `src/WS.MarkdownToPdf/Rendering/LineWrapper.cs` — word-wrapping engine (`WrapLines`, `WrapLinesPreferCommaBreak`, `WrapLinesWithSegmenter`, `SplitOnCommaOnly`, `FindMinContentWidth`)
- `src/WS.MarkdownToPdf/Rendering/InlineRenderers/InlineRenderer.cs` — extracts `TextRun` sequences from Markdig inline containers
- `tests/WS.MarkdownToPdf.Tests/Rendering/TableRendererTests.cs` — table renderer tests
