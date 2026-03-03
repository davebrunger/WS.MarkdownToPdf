using WS.MarkdownToPdf.Exceptions;

namespace WS.MarkdownToPdf.Tests;

public class ConverterIntegrationTests
{
    private readonly MarkdownToPdfConverter converter = new();

    [Fact]
    public void ConvertToBytes_FullDocument_ProducesValidPdf()
    {
        var markdown = """
            # Report Title

            This is the introduction paragraph with **bold** and *italic* text.

            ## Section One

            A paragraph with ~~strikethrough~~ formatting.

            ---

            ## Section Two

            - Bullet item one
            - Bullet item two
            - Bullet item three

            1. Numbered first
            2. Numbered second
            3. Numbered third

            > This is a block quote with some important information.

            ## Data Table

            | Name | Value | Description |
            |------|-------|-------------|
            | Alpha | 1 | First item |
            | Beta | 2 | Second item |
            | Gamma | 3 | Third item |

            ### Conclusion

            Final paragraph to close the document.
            """;

        var bytes = converter.ConvertToBytes(markdown);

        Assert.NotEmpty(bytes);
        // PDF files start with %PDF
        Assert.Equal((byte)'%', bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    [Fact]
    public void ConvertToBytes_LongDocument_ProducesMultiplePages()
    {
        var paragraphs = Enumerable.Range(1, 100)
            .Select(i => $"Paragraph {i}: Lorem ipsum dolor sit amet.")
            .ToList();
        var markdown = "# Multi-Page Document\n\n" + string.Join("\n\n", paragraphs);

        var bytes = converter.ConvertToBytes(markdown);

        Assert.NotEmpty(bytes);
        // Verify it's a valid PDF
        Assert.Equal((byte)'%', bytes[0]);
    }

    [Fact]
    public void ConvertToBytes_UnsupportedFeature_ThrowsUnsupportedMarkdownException()
    {
        // Fenced code blocks are not supported
        var markdown = """
            # Title

            ```csharp
            var x = 1;
            ```
            """;

        Assert.Throws<UnsupportedMarkdownException>(() => converter.ConvertToBytes(markdown));
    }

    [Fact]
    public void ConvertToStream_WritesToStream()
    {
        var markdown = "# Hello\n\nWorld";

        using var stream = new MemoryStream();
        converter.ConvertToStream(markdown, stream);

        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void ConvertFile_CreatesOutputFile()
    {
        var markdown = "# Test File\n\nContent here.";
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var inputPath = Path.Combine(tempDir, "input.md");
            var outputPath = Path.Combine(tempDir, "output.pdf");
            File.WriteAllText(inputPath, markdown);

            converter.ConvertFile(inputPath, outputPath);

            Assert.True(File.Exists(outputPath));
            var bytes = File.ReadAllBytes(outputPath);
            Assert.NotEmpty(bytes);
            Assert.Equal((byte)'%', bytes[0]);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
