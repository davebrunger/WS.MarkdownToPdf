using PdfSharp.Drawing;

namespace WS.MarkdownToPdf.Layout;

/// <summary>
/// Layout constants for the PDF renderer. All values are in points unless stated otherwise.
/// </summary>
public static class LayoutConstants
{
    // Page
    public const double PageWidthMm = 210;
    public const double PageHeightMm = 297;
    public const double MarginMm = 20;

    public static readonly double PageWidth = XUnit.FromMillimeter(PageWidthMm).Point;
    public static readonly double PageHeight = XUnit.FromMillimeter(PageHeightMm).Point;
    public static readonly double Margin = XUnit.FromMillimeter(MarginMm).Point;
    public static readonly double ContentWidth = PageWidth - (2 * Margin);
    public static readonly double ContentHeight = PageHeight - (2 * Margin);

    // Body font
    public const string BodyFontFamily = "Times New Roman";
    public const string MonoFontFamily = "Courier New";
    public const double BodyFontSize = 10;

    // Heading font sizes
    public static readonly double[] HeadingFontSizes = [20, 16, 14, 12, 10, 8];

    /// <summary>
    /// Returns the font size for a heading level (1–6).
    /// </summary>
    public static double GetHeadingFontSize(int level) =>
        level >= 1 && level <= 6 ? HeadingFontSizes[level - 1] : BodyFontSize;

    // Spacing
    public const double LineSpacingMultiplier = 1.2;
    public const double ParagraphSpacing = 5;

    // Lists
    public const double ListIndent = 20;

    // Block quotes
    public const double BlockQuoteIndent = 20;
    public const double BlockQuoteBarWidth = 2;
    public const double BlockQuoteBarGap = 4;

    // Tables
    public const double TableCellPadding = 4;

    // Horizontal rule
    public const double HorizontalRuleThickness = 0.5;

    // Columns
    public const double ColumnGutter = 14;
}
