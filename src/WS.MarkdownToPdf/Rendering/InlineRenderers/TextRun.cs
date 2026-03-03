using PdfSharp.Drawing;

namespace WS.MarkdownToPdf.Rendering.InlineRenderers;

/// <summary>
/// Represents a run of text with a specific font and style.
/// </summary>
/// <param name="IsLineBreak">When true, forces a new line (hard break or block-quote line break).</param>
/// <param name="IsSoftLineBreak">When true, represents a soft line break that renderers may convert to a space.</param>
public record TextRun(string Text, XFont Font, bool IsStrikethrough = false, bool IsLineBreak = false, bool IsSoftLineBreak = false);
