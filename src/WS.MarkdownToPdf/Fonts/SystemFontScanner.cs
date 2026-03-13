using System.Text;

namespace WS.MarkdownToPdf.Fonts;

/// <summary>
/// Scans system font directories and discovers installed TrueType font families
/// by parsing the TTF name and head tables.
/// </summary>
public static class SystemFontScanner
{
    /// <summary>
    /// Returns the distinct font family names installed on the system, sorted alphabetically.
    /// </summary>
    public static IReadOnlyList<string> GetInstalledFontFamilies()
    {
        return ScanFontVariants().Keys.Order().ToList();
    }

    /// <summary>
    /// Scans system fonts and returns a map of family name to file paths per style.
    /// Each value is a 4-element array indexed by: 0 = regular, 1 = bold, 2 = italic, 3 = bold-italic.
    /// Entries may be null if that variant is not available.
    /// </summary>
    internal static Dictionary<string, string?[]> ScanFontVariants()
    {
        var families = new Dictionary<string, string?[]>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in GetFontDirectories())
        {
            if (!Directory.Exists(dir)) continue;

            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories); }
            catch { continue; }

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                if (!ext.Equals(".ttf", StringComparison.OrdinalIgnoreCase) &&
                    !ext.Equals(".otf", StringComparison.OrdinalIgnoreCase))
                    continue;

                var info = TryReadFontInfo(file);
                if (info is null) continue;

                if (!families.TryGetValue(info.Value.FamilyName, out var variants))
                {
                    variants = new string?[4];
                    families[info.Value.FamilyName] = variants;
                }

                var index = (info.Value.IsBold ? 1 : 0) + (info.Value.IsItalic ? 2 : 0);
                variants[index] ??= file;
            }
        }

        return families;
    }

    private static IEnumerable<string> GetFontDirectories()
    {
        if (OperatingSystem.IsWindows())
        {
            yield return Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return Path.Combine(localAppData, @"Microsoft\Windows\Fonts");
        }
        else if (OperatingSystem.IsMacOS())
        {
            yield return "/Library/Fonts";
            yield return "/System/Library/Fonts";
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, "Library/Fonts");
        }
        else
        {
            yield return "/usr/share/fonts";
            yield return "/usr/local/share/fonts";
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".local/share/fonts");
            yield return Path.Combine(home, ".fonts");
        }
    }

    internal static (string FamilyName, bool IsBold, bool IsItalic)? TryReadFontInfo(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            if (fs.Length < 12) return null;
            using var reader = new BinaryReader(fs);

            ReadUInt32BE(reader); // sfVersion
            var numTables = ReadUInt16BE(reader);
            reader.ReadBytes(6); // searchRange, entrySelector, rangeShift

            uint nameTableOffset = 0;
            uint headTableOffset = 0;

            for (var i = 0; i < numTables; i++)
            {
                if (fs.Position + 16 > fs.Length) return null;
                var tag = Encoding.ASCII.GetString(reader.ReadBytes(4));
                ReadUInt32BE(reader); // checksum
                var offset = ReadUInt32BE(reader);
                ReadUInt32BE(reader); // length

                if (tag == "name") nameTableOffset = offset;
                else if (tag == "head") headTableOffset = offset;
            }

            if (nameTableOffset == 0 || headTableOffset == 0) return null;

            var familyName = ReadFamilyName(fs, reader, nameTableOffset);
            if (familyName is null) return null;

            // macStyle is at offset 44 in the head table (bit 0 = bold, bit 1 = italic)
            if (headTableOffset + 46 > (uint)fs.Length) return null;
            fs.Seek(headTableOffset + 44, SeekOrigin.Begin);
            var macStyle = ReadUInt16BE(reader);

            return (familyName, IsBold: (macStyle & 1) != 0, IsItalic: (macStyle & 2) != 0);
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadFamilyName(Stream fs, BinaryReader reader, uint nameTableOffset)
    {
        fs.Seek(nameTableOffset, SeekOrigin.Begin);
        ReadUInt16BE(reader); // format
        var count = ReadUInt16BE(reader);
        var stringOffset = ReadUInt16BE(reader);

        string? windowsName = null;
        string? macName = null;

        for (var i = 0; i < count; i++)
        {
            if (fs.Position + 12 > fs.Length) break;

            var platformID = ReadUInt16BE(reader);
            ReadUInt16BE(reader); // encodingID
            ReadUInt16BE(reader); // languageID
            var nameID = ReadUInt16BE(reader);
            var length = ReadUInt16BE(reader);
            var strOffset = ReadUInt16BE(reader);

            if (nameID != 1) continue;

            var pos = fs.Position;
            var targetPos = nameTableOffset + stringOffset + strOffset;
            if (targetPos + length > fs.Length) { fs.Seek(pos, SeekOrigin.Begin); continue; }

            fs.Seek(targetPos, SeekOrigin.Begin);
            var data = reader.ReadBytes(length);
            fs.Seek(pos, SeekOrigin.Begin);

            if (platformID == 3 && windowsName is null)
                windowsName = Encoding.BigEndianUnicode.GetString(data);
            else if (platformID == 1 && macName is null)
                macName = Encoding.ASCII.GetString(data);

            if (windowsName is not null) break;
        }

        return windowsName ?? macName;
    }

    private static ushort ReadUInt16BE(BinaryReader reader)
    {
        var b = reader.ReadBytes(2);
        return (ushort)((b[0] << 8) | b[1]);
    }

    private static uint ReadUInt32BE(BinaryReader reader)
    {
        var b = reader.ReadBytes(4);
        return (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
    }
}
