---
name: pdfsharp
description: Guidance for generating and processing PDF documents in .NET using PDFsharp — document creation, drawing API, text layout, images, fonts, page management, and testing patterns.
---

# PDF Generation with PDFsharp

## Library

Use **PDFsharp** — an open-source .NET library for creating and processing PDF documents.

```shell
dotnet add package PDFsharp
```

- **Version**: 6.2.x (latest stable)
- **License**: MIT
- **Docs**: <https://docs.pdfsharp.net>
- **Source**: <https://github.com/empira/PDFsharp>
- **Targets**: .NET 8 / .NET 9 / .NET 10 and .NET Framework 4.6.2

> PDFsharp provides low-level drawing primitives (similar to GDI+). For document-level abstractions (paragraphs, tables, styles, automatic page breaks), consider **MigraDoc** which ships alongside PDFsharp.

---

## Core Concepts

| Type | Purpose |
|------|---------|
| `PdfDocument` | Represents a PDF file — create new or open existing |
| `PdfPage` | A single page within a document |
| `XGraphics` | Drawing surface for a page (analogous to `System.Drawing.Graphics`) |
| `XFont` | Font definition for text rendering |
| `XBrush` / `XPen` | Fill and stroke styles |
| `XImage` | Bitmap or vector image to draw on a page |
| `XRect` / `XPoint` / `XSize` | Geometry primitives |
| `XUnit` | Measurement unit (point, millimetre, centimetre, inch) |

---

## Creating a PDF Document

```csharp
using PdfSharp.Pdf;
using PdfSharp.Drawing;

var document = new PdfDocument();
document.Info.Title = "My Document";

var page = document.AddPage();
page.Size = PdfSharp.PageSize.A4;

var gfx = XGraphics.FromPdfPage(page);
var font = new XFont("Arial", 20, XFontStyleEx.Bold);

gfx.DrawString("Hello, PDFsharp!", font, XBrushes.Black,
    new XRect(0, 0, page.Width.Point, page.Height.Point),
    XStringFormats.Center);

document.Save("output.pdf");
```

---

## Page Setup

```csharp
var page = document.AddPage();
page.Size = PdfSharp.PageSize.A4;          // or Letter, Legal, etc.
page.Orientation = PdfSharp.PageOrientation.Portrait;

// Custom size (in points: 1 inch = 72 points)
page.Width = XUnit.FromMillimeter(210);
page.Height = XUnit.FromMillimeter(297);
```

- Always set page size **before** creating `XGraphics`
- Use `XUnit` for explicit unit conversions: `XUnit.FromMillimeter()`, `XUnit.FromCentimeter()`, `XUnit.FromInch()`

---

## Drawing Text

```csharp
var font = new XFont("Segoe UI", 12, XFontStyleEx.Regular);

// Simple draw at position
gfx.DrawString("Hello", font, XBrushes.Black, new XPoint(40, 100));

// Draw within a rectangle with alignment
gfx.DrawString("Centred text", font, XBrushes.Black,
    new XRect(40, 100, 200, 50),
    XStringFormats.Center);
```

### Text measurement

```csharp
var size = gfx.MeasureString("Hello", font);
// size.Width, size.Height — use for manual layout calculations
```

- PDFsharp does **not** provide automatic text wrapping or page breaks — you must calculate line positions manually or use MigraDoc
- For multi-line text, split into lines and draw each with an incremented Y offset

---

## Drawing Shapes

```csharp
var pen = new XPen(XColors.DarkBlue, 1.5);
var brush = new XSolidBrush(XColor.FromArgb(128, 0, 0, 255));

// Rectangle
gfx.DrawRectangle(pen, brush, 40, 100, 200, 50);

// Ellipse
gfx.DrawEllipse(pen, 40, 200, 200, 100);

// Line
gfx.DrawLine(pen, new XPoint(40, 350), new XPoint(240, 350));

// Rounded rectangle
gfx.DrawRoundedRectangle(pen, brush, new XRect(40, 400, 200, 50), new XSize(10, 10));
```

---

## Drawing Images

```csharp
var image = XImage.FromFile("logo.png");

// Draw at position with original size
gfx.DrawImage(image, 40, 40);

// Draw scaled into a rectangle
gfx.DrawImage(image, new XRect(40, 40, 150, 100));
```

- Supported formats: PNG, JPEG, BMP, GIF
- For streams: `XImage.FromStream(stream)`
- Dispose images when done to free memory

---

## Fonts

```csharp
// System font
var font = new XFont("Arial", 12, XFontStyleEx.Regular);

// Bold + Italic
var boldItalic = new XFont("Arial", 14, XFontStyleEx.BoldItalic);
```

### Font resolver (for cross-platform / embedded fonts)

On non-Windows platforms, register a custom font resolver:

```csharp
GlobalFontSettings.FontResolver = new MyFontResolver();

public class MyFontResolver : IFontResolver
{
    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        // Return a FontResolverInfo pointing to your font
    }

    public byte[]? GetFont(string faceName)
    {
        // Return font file bytes
    }
}
```

- Always set `GlobalFontSettings.FontResolver` **before** creating any documents
- Embed fonts for reliable rendering across platforms

---

## Working with Existing PDFs

```csharp
// Open an existing PDF
var document = PdfReader.Open("existing.pdf", PdfDocumentOpenMode.Modify);

// Draw on an existing page
var page = document.Pages[0];
var gfx = XGraphics.FromPdfPage(page);
gfx.DrawString("Watermark", font, XBrushes.LightGray,
    new XRect(0, 0, page.Width.Point, page.Height.Point),
    XStringFormats.Center);

document.Save("modified.pdf");
```

### Open modes

| Mode | Purpose |
|------|---------|
| `PdfDocumentOpenMode.Modify` | Read and write — saves back to file |
| `PdfDocumentOpenMode.Import` | Read-only — for extracting/copying pages |
| `PdfDocumentOpenMode.ReadOnly` | Read-only — for inspection |

---

## Merging PDFs

```csharp
var output = new PdfDocument();

foreach (var file in pdfFiles)
{
    var input = PdfReader.Open(file, PdfDocumentOpenMode.Import);
    foreach (var page in input.Pages)
    {
        output.AddPage(page);
    }
}

output.Save("merged.pdf");
```

---

## Coordinate System

- Origin `(0, 0)` is the **top-left** corner of the page
- X increases to the right, Y increases **downward**
- All measurements default to **points** (1 point = 1/72 inch)
- Use `XUnit` for conversions: `XUnit.FromMillimeter(10).Point`

---

## Design Guidance

- **Inject an abstraction** — define an `IPdfGenerator` interface in your domain; implement with PDFsharp in infrastructure
- **Separate layout from rendering** — compute positions/sizes in a layout model, then draw in a single pass
- **Dispose `XGraphics`** — use `using` statements or `await using` for cleanup
- **Save to streams** for testability — `document.Save(stream, closeStream: false)` avoids file I/O in tests
- **Use MigraDoc** for document-oriented output (paragraphs, tables, automatic page breaks) — it renders via PDFsharp under the hood
- PDFsharp does **not** sanitise or validate content — ensure input strings are clean before rendering

---

## Testing

```csharp
public class PdfGeneratorTests
{
    [Fact]
    public void Generate_CreatesNonEmptyPdf()
    {
        // Arrange
        using var stream = new MemoryStream();

        // Act
        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawString("Test", new XFont("Arial", 12), XBrushes.Black, new XPoint(10, 10));
        document.Save(stream, closeStream: false);

        // Assert
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void Generate_AddsCorrectNumberOfPages()
    {
        var document = new PdfDocument();
        document.AddPage();
        document.AddPage();

        Assert.Equal(2, document.PageCount);
    }

    [Theory]
    [InlineData(PdfSharp.PageSize.A4, 595.276, 841.890)]
    [InlineData(PdfSharp.PageSize.Letter, 612, 792)]
    public void Page_HasExpectedDimensions(PdfSharp.PageSize size, double expectedWidth, double expectedHeight)
    {
        var document = new PdfDocument();
        var page = document.AddPage();
        page.Size = size;

        Assert.Equal(expectedWidth, page.Width.Point, precision: 0);
        Assert.Equal(expectedHeight, page.Height.Point, precision: 0);
    }
}
```

---

## References

- Documentation: <https://docs.pdfsharp.net>
- Samples: <https://docs.pdfsharp.net/PDFsharp/Overview/About.html>
- GitHub: <https://github.com/empira/PDFsharp>
- MigraDoc (document-level API): <https://docs.pdfsharp.net/MigraDoc/Overview/About.html>
