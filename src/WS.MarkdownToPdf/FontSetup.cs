using PdfSharp.Fonts;

namespace WS.MarkdownToPdf;

/// <summary>
/// Ensures the font resolver is registered before any PDFsharp font operations.
/// </summary>
internal static class FontSetup
{
    private static bool _initialized;
    private static readonly object _lock = new();

    /// <summary>
    /// Registers the system font resolver. Safe to call multiple times.
    /// </summary>
    internal static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            _initialized = true;
            GlobalFontSettings.FontResolver = new Fonts.SystemFontResolver();
        }
    }
}
