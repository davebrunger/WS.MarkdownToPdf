using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Parsing;
using WS.MarkdownToPdf.Rendering;

namespace WS.MarkdownToPdf;

/// <summary>
/// Public entry point for converting Markdown to PDF.
/// Accepts an optional <see cref="LayoutOptions"/> to customise page size, fonts,
/// spacing, and other layout parameters.
/// </summary>
public class MarkdownToPdfConverter
{
    private readonly IMarkdownParser parser = new MarkdigParser();
    private readonly IPdfRenderer renderer = new PdfRenderer();
    private readonly LayoutOptions? layout;

    /// <summary>
    /// Creates a converter that uses the default layout.
    /// </summary>
    public MarkdownToPdfConverter() { }

    /// <summary>
    /// Creates a converter with custom layout options.
    /// </summary>
    public MarkdownToPdfConverter(LayoutOptions layout) => this.layout = layout;

    /// <summary>
    /// Converts a Markdown string to PDF and writes it to the given stream.
    /// </summary>
    public void ConvertToStream(string markdown, Stream output)
    {
        ArgumentNullException.ThrowIfNull(markdown);
        ArgumentNullException.ThrowIfNull(output);

        var document = parser.Parse(markdown);
        using var pdf = renderer.Render(document, layout);
        pdf.Save(output, false);
    }

    /// <summary>
    /// Converts a Markdown string to a PDF byte array.
    /// </summary>
    public byte[] ConvertToBytes(string markdown)
    {
        using var stream = new MemoryStream();
        ConvertToStream(markdown, stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Converts a Markdown file to a PDF file.
    /// </summary>
    public void ConvertFile(string inputPath, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(inputPath);
        ArgumentNullException.ThrowIfNull(outputPath);

        var markdown = File.ReadAllText(inputPath);
        using var output = File.Create(outputPath);
        ConvertToStream(markdown, output);
    }
}
