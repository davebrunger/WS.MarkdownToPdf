using Markdig.Extensions.Tables;
using Markdig.Syntax;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Exceptions;
using WS.MarkdownToPdf.Rendering.BlockRenderers;

namespace WS.MarkdownToPdf.Rendering;

/// <summary>
/// Walks the Markdig AST and dispatches each block to the appropriate renderer.
/// </summary>
public class PdfRenderer : IPdfRenderer
{
    private readonly HeadingRenderer headingRenderer = new();
    private readonly ParagraphRenderer paragraphRenderer = new();
    private readonly ThematicBreakRenderer thematicBreakRenderer = new();
    private readonly ListRenderer listRenderer = new();
    private readonly QuoteBlockRenderer quoteBlockRenderer = new();
    private readonly TableRenderer tableRenderer = new();

    public PdfRenderer()
    {
        FontSetup.EnsureInitialized();
    }

    /// <inheritdoc />
    public PdfDocument Render(MarkdownDocument document)
    {
        var pdf = new PdfDocument();
        var context = new RenderContext(pdf);

        var blocks = document.ToList();
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            RenderBlock(block, blocks, i, context);
        }

        return pdf;
    }

    private void RenderBlock(Block block, List<Block> blocks, int index, RenderContext context)
    {
        switch (block)
        {
            case HeadingBlock heading:
                headingRenderer.Render(heading, blocks, index, context);
                break;
            case ParagraphBlock paragraph:
                paragraphRenderer.Render(paragraph, context);
                break;
            case ThematicBreakBlock thematicBreak:
                thematicBreakRenderer.Render(thematicBreak, context);
                break;
            case ListBlock list:
                listRenderer.Render(list, context);
                break;
            case QuoteBlock quote:
                quoteBlockRenderer.Render(quote, context);
                break;
            case Table table:
                tableRenderer.Render(table, context);
                break;
            default:
                throw new UnsupportedMarkdownException(block.GetType().Name);
        }
    }
}
