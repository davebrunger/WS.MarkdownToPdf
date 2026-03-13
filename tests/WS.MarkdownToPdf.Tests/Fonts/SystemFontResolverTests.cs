using PdfSharp.Fonts;
using WS.MarkdownToPdf.Fonts;

namespace WS.MarkdownToPdf.Tests.Fonts;

public class SystemFontResolverTests
{
    private readonly SystemFontResolver resolver = new();

    [Fact]
    public void ResolveTypeface_KnownFamily_ReturnsFontResolverInfo()
    {
        var info = resolver.ResolveTypeface("Times New Roman", false, false);

        Assert.NotNull(info);
    }

    [Fact]
    public void ResolveTypeface_UnknownFamily_ReturnsNull()
    {
        var info = resolver.ResolveTypeface("NonExistentFontFamily_XYZ_999", false, false);

        Assert.Null(info);
    }

    [Fact]
    public void ResolveTypeface_Bold_ReturnsDifferentFaceNameThanRegular()
    {
        var regular = resolver.ResolveTypeface("Times New Roman", false, false);
        var bold = resolver.ResolveTypeface("Times New Roman", true, false);

        Assert.NotNull(regular);
        Assert.NotNull(bold);
        Assert.NotEqual(regular.FaceName, bold.FaceName);
    }

    [Fact]
    public void ResolveTypeface_Italic_ReturnsDifferentFaceNameThanRegular()
    {
        var regular = resolver.ResolveTypeface("Times New Roman", false, false);
        var italic = resolver.ResolveTypeface("Times New Roman", false, true);

        Assert.NotNull(regular);
        Assert.NotNull(italic);
        Assert.NotEqual(regular.FaceName, italic.FaceName);
    }

    [Fact]
    public void GetFont_ValidFaceName_ReturnsFontBytes()
    {
        var info = resolver.ResolveTypeface("Times New Roman", false, false);
        Assert.NotNull(info);

        var bytes = resolver.GetFont(info.FaceName);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void GetFont_InvalidFaceName_ReturnsNull()
    {
        var bytes = resolver.GetFont("bogus");

        Assert.Null(bytes);
    }

    [Fact]
    public void GetFont_UnknownFamily_ReturnsNull()
    {
        var bytes = resolver.GetFont("NonExistentFont#0");

        Assert.Null(bytes);
    }

    [Fact]
    public void GetFont_MalformedFaceName_ReturnsNull()
    {
        var bytes = resolver.GetFont("no-hash-separator");

        Assert.Null(bytes);
    }
}
