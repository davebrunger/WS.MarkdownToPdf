using PdfSharp.Drawing;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Layout;

namespace WS.MarkdownToPdf.Rendering;

/// <summary>
/// Tracks the current drawing state: page, Y position, graphics surface, and fonts.
/// </summary>
public class RenderContext
{
    public PdfDocument Document { get; }
    public PdfPage CurrentPage { get; private set; }
    public XGraphics Graphics { get; private set; }
    public double CurrentY { get; set; }

    public double ContentLeft => LayoutConstants.Margin;
    public double ContentRight => LayoutConstants.PageWidth - LayoutConstants.Margin;
    public double ContentWidth => LayoutConstants.ContentWidth;
    public double ContentTop => LayoutConstants.Margin;
    public double ContentBottom => LayoutConstants.PageHeight - LayoutConstants.Margin;
    public double RemainingHeight => ContentBottom - CurrentY;

    public RenderContext(PdfDocument document)
    {
        Document = document;
        CurrentPage = AddNewPage();
        Graphics = XGraphics.FromPdfPage(CurrentPage);
        CurrentY = ContentTop;
    }

    /// <summary>
    /// Starts a new page and resets the Y position to the top margin.
    /// </summary>
    public void AddPage()
    {
        Graphics.Dispose();
        CurrentPage = AddNewPage();
        Graphics = XGraphics.FromPdfPage(CurrentPage);
        CurrentY = ContentTop;
    }

    /// <summary>
    /// If the required height does not fit on the current page, adds a new page.
    /// </summary>
    public void EnsureSpace(double requiredHeight)
    {
        if (CurrentY + requiredHeight > ContentBottom)
        {
            AddPage();
        }
    }

    private PdfPage AddNewPage()
    {
        var page = Document.AddPage();
        page.Width = XUnit.FromMillimeter(LayoutConstants.PageWidthMm);
        page.Height = XUnit.FromMillimeter(LayoutConstants.PageHeightMm);
        return page;
    }
}
