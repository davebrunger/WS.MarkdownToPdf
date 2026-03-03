using PdfSharp.Drawing;

namespace WS.MarkdownToPdf.Rendering.InlineRenderers;

/// <summary>
/// Represents a run of text with a specific font and style.
/// </summary>
public record TextRun(string Text, XFont Font, bool IsStrikethrough = false);
