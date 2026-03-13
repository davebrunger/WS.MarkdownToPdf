global using Xunit;

using System.Runtime.CompilerServices;
using WS.MarkdownToPdf;

namespace WS.MarkdownToPdf.Tests;

internal static class TestInitializer
{
    [ModuleInitializer]
    internal static void Initialize() => FontSetup.EnsureInitialized();
}