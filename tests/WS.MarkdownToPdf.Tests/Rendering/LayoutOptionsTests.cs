using WS.MarkdownToPdf.Layout;
using WS.MarkdownToPdf.Parsing;
using WS.MarkdownToPdf.Rendering;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class LayoutOptionsTests
{
    [Fact]
    public void IsLandscape_SwapsPageDimensions()
    {
        var portrait = new LayoutOptions();
        var landscape = new LayoutOptions { IsLandscape = true };

        Assert.Equal(portrait.PageWidth, landscape.PageHeight);
        Assert.Equal(portrait.PageHeight, landscape.PageWidth);
    }

    [Fact]
    public void IsLandscape_ContentWidthIsWider()
    {
        var portrait = new LayoutOptions();
        var landscape = new LayoutOptions { IsLandscape = true };

        Assert.True(landscape.ContentWidth > portrait.ContentWidth);
    }

    [Fact]
    public void EffectiveHeadingFontFamily_DefaultsToBodyFont()
    {
        var options = new LayoutOptions();

        Assert.Equal(options.BodyFontFamily, options.EffectiveHeadingFontFamily);
    }

    [Fact]
    public void EffectiveHeadingFontFamily_ReturnsOverrideWhenSet()
    {
        var options = new LayoutOptions { HeadingFontFamily = "Arial" };

        Assert.Equal("Arial", options.EffectiveHeadingFontFamily);
    }

    [Fact]
    public void Landscape_Render_ProducesValidPdf()
    {
        var renderer = new PdfRenderer();
        var parser = new MarkdigParser();
        var doc = parser.Parse("# Heading\n\nSome body text.");

        using var pdf = renderer.Render(doc, new LayoutOptions { IsLandscape = true });

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void HeadingFont_Render_ProducesValidPdf()
    {
        var renderer = new PdfRenderer();
        var parser = new MarkdigParser();
        var doc = parser.Parse("# Heading\n\nSome body text.");

        using var pdf = renderer.Render(doc, new LayoutOptions { HeadingFontFamily = "Arial" });

        Assert.Equal(1, pdf.PageCount);
        using var stream = new MemoryStream();
        pdf.Save(stream, false);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void MaxJustificationGap_HasSensibleDefault()
    {
        var options = new LayoutOptions();

        Assert.True(options.MaxJustificationGap > 0);
        Assert.True(options.MaxJustificationGap <= 10);
    }
}
