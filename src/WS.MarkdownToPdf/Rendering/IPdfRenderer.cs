using Markdig.Syntax;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Layout;

namespace WS.MarkdownToPdf.Rendering;

/// <summary>
/// Renders a Markdig <see cref="MarkdownDocument"/> to a <see cref="PdfDocument"/>.
/// </summary>
public interface IPdfRenderer
{
    /// <summary>
    /// Renders the given Markdown AST and returns a PDF document.
    /// </summary>
    PdfDocument Render(MarkdownDocument document, LayoutOptions? layout = null);
}
