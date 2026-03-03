using PdfSharp.Fonts;

namespace WS.MarkdownToPdf.Fonts;

/// <summary>
/// Resolves fonts from the Windows system fonts directory for PDFsharp 6.x core package.
/// </summary>
public class SystemFontResolver : IFontResolver
{
    private static readonly string FontsDir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    private static readonly Dictionary<string, string[]> FontFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Times New Roman"] = ["times.ttf", "timesbd.ttf", "timesi.ttf", "timesbi.ttf"],
        ["Courier New"] = ["cour.ttf", "courbd.ttf", "couri.ttf", "courbi.ttf"],
    };

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (!FontFiles.ContainsKey(familyName))
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
        if (!FontFiles.TryGetValue(familyName, out var files)) return null;
        if (index < 0 || index >= files.Length) return null;

        var path = Path.Combine(FontsDir, files[index]);
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }
}
