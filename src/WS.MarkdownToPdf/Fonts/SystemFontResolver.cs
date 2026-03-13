using PdfSharp.Fonts;

namespace WS.MarkdownToPdf.Fonts;

/// <summary>
/// Resolves fonts from system font directories for PDFsharp 6.x core package.
/// Dynamically discovers installed TrueType fonts via <see cref="SystemFontScanner"/>.
/// </summary>
public class SystemFontResolver : IFontResolver
{
    private readonly Dictionary<string, string?[]> _fontVariants = SystemFontScanner.ScanFontVariants();

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (!_fontVariants.ContainsKey(familyName))
            return null;

        var index = (isBold ? 1 : 0) + (isItalic ? 2 : 0);
        var faceName = $"{familyName}#{index}";
        return new FontResolverInfo(faceName);
    }

    public byte[]? GetFont(string faceName)
    {
        var parts = faceName.Split('#');
        if (parts.Length != 2) return null;

        var familyName = parts[0];
        if (!int.TryParse(parts[1], out var index)) return null;
        if (!_fontVariants.TryGetValue(familyName, out var variants)) return null;
        if (index < 0 || index >= variants.Length) return null;

        // Fall back to regular variant when the requested style is not available
        var path = variants[index] ?? variants[0];
        if (path is null) return null;

        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }
}
