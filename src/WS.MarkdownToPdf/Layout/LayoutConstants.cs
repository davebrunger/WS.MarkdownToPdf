namespace WS.MarkdownToPdf.Layout;

/// <summary>
/// Provides static access to the default <see cref="LayoutOptions"/> values.
/// Kept for backward compatibility — all rendering code reads from
/// the <see cref="LayoutOptions"/> instance on <c>RenderContext</c>.
/// </summary>
public static class LayoutConstants
{
    /// <summary>
    /// The default layout options instance used when no custom options are supplied.
    /// </summary>
    public static LayoutOptions Default { get; } = new();

    // Page
    public static double PageWidthMm => Default.PageWidthMm;
    public static double PageHeightMm => Default.PageHeightMm;
    public static double MarginMm => Default.MarginMm;
    public static double PageWidth => Default.PageWidth;
    public static double PageHeight => Default.PageHeight;
    public static double Margin => Default.Margin;
    public static double ContentWidth => Default.ContentWidth;
    public static double ContentHeight => Default.ContentHeight;

    // Body font
    public static string BodyFontFamily => Default.BodyFontFamily;
    public static string MonoFontFamily => Default.MonoFontFamily;
    public static double BodyFontSize => Default.BodyFontSize;

    // Heading font sizes
    public static double GetHeadingFontSize(int level) => Default.GetHeadingFontSize(level);

    // Spacing
    public static double LineSpacingMultiplier => Default.LineSpacingMultiplier;
    public static double ParagraphSpacing => Default.ParagraphSpacing;

    // Lists
    public static double ListIndent => Default.ListIndent;
    public static double ListItemSpacing => Default.ListItemSpacing;

    // Block quotes
    public static double BlockQuoteIndent => Default.BlockQuoteIndent;
    public static double BlockQuoteBarWidth => Default.BlockQuoteBarWidth;
    public static double BlockQuoteBarGap => Default.BlockQuoteBarGap;
    public static string BlockQuoteBarColor => Default.BlockQuoteBarColor;

    // Tables
    public static double TableCellPaddingH => Default.TableCellPaddingH;
    public static double TableCellPaddingV => Default.TableCellPaddingV;
    public static double TableHeaderRuleThickness => Default.TableHeaderRuleThickness;

    // Strikethrough
    public static double StrikethroughOffsetRatio => Default.StrikethroughOffsetRatio;

    // Horizontal rule
    public static double HorizontalRuleThickness => Default.HorizontalRuleThickness;

    // Columns
    public static double ColumnGutter => Default.ColumnGutter;

    // Page numbers
    public static double PageNumberFontSize => Default.PageNumberFontSize;
}
