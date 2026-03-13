using WS.MarkdownToPdf.Fonts;

namespace WS.MarkdownToPdf.Tests.Rendering;

public class SystemFontScannerTests
{
    [Fact]
    public void GetInstalledFontFamilies_ReturnsNonEmptyList()
    {
        var families = SystemFontScanner.GetInstalledFontFamilies();

        Assert.NotEmpty(families);
    }

    [Fact]
    public void GetInstalledFontFamilies_ReturnsSortedList()
    {
        var families = SystemFontScanner.GetInstalledFontFamilies();

        for (var i = 1; i < families.Count; i++)
        {
            Assert.True(
                string.Compare(families[i - 1], families[i], StringComparison.OrdinalIgnoreCase) <= 0,
                $"'{families[i - 1]}' should sort before '{families[i]}'");
        }
    }

    [Fact]
    public void GetInstalledFontFamilies_ContainsTimesNewRoman()
    {
        var families = SystemFontScanner.GetInstalledFontFamilies();

        Assert.Contains("Times New Roman", families);
    }

    [Fact]
    public void GetInstalledFontFamilies_ContainsCourierNew()
    {
        var families = SystemFontScanner.GetInstalledFontFamilies();

        Assert.Contains("Courier New", families);
    }

    [Fact]
    public void ScanFontVariants_ReturnsVariantsForKnownFont()
    {
        var variants = SystemFontScanner.ScanFontVariants();

        Assert.True(variants.ContainsKey("Times New Roman"));
        var files = variants["Times New Roman"];
        Assert.Equal(4, files.Length);
        Assert.NotNull(files[0]); // Regular variant should exist
    }
}
