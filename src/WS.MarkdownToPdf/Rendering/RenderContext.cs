using PdfSharp.Drawing;
using PdfSharp.Pdf;
using WS.MarkdownToPdf.Layout;

namespace WS.MarkdownToPdf.Rendering;

/// <summary>
/// Tracks the current drawing state: page, Y position, graphics surface, and fonts.
/// Supports optional column-layout overrides for multi-column rendering.
/// </summary>
public class RenderContext
{
    private double? columnLeft;
    private double? columnWidth;

    public PdfDocument Document { get; }
    public LayoutOptions Layout { get; }
    public PdfPage CurrentPage { get; private set; }
    public XGraphics Graphics { get; private set; }
    public double CurrentY { get; set; }

    public double ContentLeft => columnLeft ?? Layout.Margin;
    public double ContentRight => ContentLeft + ContentWidth;
    public double ContentWidth => columnWidth ?? Layout.ContentWidth;
    public double ContentTop => Layout.Margin;
    public double ContentBottom => Layout.PageHeight - Layout.Margin;
    public double RemainingHeight => ContentBottom - CurrentY;

    /// <summary>
    /// True when rendering inside a multi-column section.
    /// </summary>
    public bool IsInColumnLayout => columnLeft.HasValue;

    public RenderContext(PdfDocument document, LayoutOptions? layout = null)
    {
        Document = document;
        Layout = layout ?? LayoutConstants.Default;
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
        page.Width = XUnit.FromMillimeter(Layout.PageWidthMm);
        page.Height = XUnit.FromMillimeter(Layout.PageHeightMm);
        return page;
    }

    /// <summary>
    /// Constrains the content area to a single column.
    /// </summary>
    public void SetColumnLayout(double left, double width)
    {
        columnLeft = left;
        columnWidth = width;
    }

    /// <summary>
    /// Restores the full-width content area.
    /// </summary>
    public void ClearColumnLayout()
    {
        columnLeft = null;
        columnWidth = null;
    }
}
