namespace WS.MarkdownToPdf.Layout;

/// <summary>
/// Configurable layout options for the PDF renderer.
/// All distance values are in points unless stated otherwise.
/// Create with object initializer syntax to override any defaults:
/// <code>
/// var options = new LayoutOptions { BodyFontSize = 12, MarginMm = 25 };
/// </code>
/// </summary>
public class LayoutOptions
{
    // Page
    public double PageWidthMm { get; init; } = 210;
    public double PageHeightMm { get; init; } = 297;
    public double MarginMm { get; init; } = 20;
    public bool IsLandscape { get; init; }

    // Computed page dimensions (in points). 1 mm = 72 / 25.4 pt.
    private const double PointsPerMm = 72.0 / 25.4;
    public double PageWidth => (IsLandscape ? PageHeightMm : PageWidthMm) * PointsPerMm;
    public double PageHeight => (IsLandscape ? PageWidthMm : PageHeightMm) * PointsPerMm;
    public double Margin => MarginMm * PointsPerMm;
    public double ContentWidth => PageWidth - (2 * Margin);
    public double ContentHeight => PageHeight - (2 * Margin);

    // Body font
    public string BodyFontFamily { get; init; } = "Times New Roman";
    public string? HeadingFontFamily { get; init; }
    public string MonoFontFamily { get; init; } = "Courier New";
    public double BodyFontSize { get; init; } = 10;

    /// <summary>
    /// Returns the heading font family, falling back to <see cref="BodyFontFamily"/> when not set.
    /// </summary>
    public string EffectiveHeadingFontFamily => HeadingFontFamily ?? BodyFontFamily;

    // Heading font sizes — ratios relative to body font size (H1 … H6)
    private static readonly double[] HeadingRatios = [2.0, 1.6, 1.4, 1.2, 1.0, 0.8];

    /// <summary>
    /// Returns the font size for a heading level (1–6), scaled from <see cref="BodyFontSize"/>.
    /// </summary>
    public double GetHeadingFontSize(int level) =>
        level >= 1 && level <= HeadingRatios.Length
            ? BodyFontSize * HeadingRatios[level - 1]
            : BodyFontSize;

    // Spacing
    public double LineSpacingMultiplier { get; init; } = 1.2;
    public double ParagraphSpacing { get; init; } = 5;

    // Lists
    public double ListIndent { get; init; } = 20;
    public double ListItemSpacing { get; init; } = 2;

    // Block quotes
    public double BlockQuoteIndent { get; init; } = 20;
    public double BlockQuoteBarWidth { get; init; } = 2;
    public double BlockQuoteBarGap { get; init; } = 4;

    /// <summary>
    /// Hex colour string for the block-quote left bar (e.g. "#D3D3D3").
    /// </summary>
    public string BlockQuoteBarColor { get; init; } = "#D3D3D3";

    // Tables
    public double TableCellPaddingH { get; init; } = 4;
    public double TableCellPaddingV { get; init; } = 2;
    public double TableHeaderRuleThickness { get; init; } = 0.5;

    // Strikethrough
    public double StrikethroughOffsetRatio { get; init; } = 0.35;

    // Horizontal rule
    public double HorizontalRuleThickness { get; init; } = 0.5;

    // Columns
    public double ColumnGutter { get; init; } = 14;

    // Page numbers
    public double PageNumberFontSize { get; init; } = 8;

    // Computed page dimensions (in mm → points) use PdfSharp's XUnit internally,
    // but callers never need to reference PdfSharp types.
}
