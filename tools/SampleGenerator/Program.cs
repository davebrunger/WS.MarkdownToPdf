using System.CommandLine;
using WS.MarkdownToPdf;
using WS.MarkdownToPdf.Fonts;
using WS.MarkdownToPdf.Layout;

// --- generate command ---
var inputArg = new Argument<FileInfo>("input") { Description = "Markdown file to convert", DefaultValueFactory = _ => new FileInfo("sample.md") };
var outputArg = new Argument<FileInfo>("output") { Description = "PDF output path", DefaultValueFactory = _ => new FileInfo("sample.pdf") };
var fontSizeOption = new Option<double>("--font-size", "-fs") { Description = "Body font size in points", DefaultValueFactory = _ => 10 };
var fontOption = new Option<string>("--font", "-f") { Description = "Body font family name (use 'fonts' command to list available fonts)" };
var headingFontOption = new Option<string>("--heading-font", "-fh") { Description = "Heading font family name (defaults to body font)" };
var orientationOption = new Option<string>("--orientation", "-o") { Description = "Page orientation: portrait or landscape", DefaultValueFactory = _ => "portrait" };
orientationOption.AcceptOnlyFromAmong("portrait", "landscape");

var generateCommand = new Command("generate", "Convert a Markdown file to PDF")
{
    inputArg,
    outputArg,
    fontSizeOption,
    fontOption,
    headingFontOption,
    orientationOption
};

generateCommand.SetAction(parseResult =>
{
    var input = parseResult.GetValue(inputArg)!;
    var output = parseResult.GetValue(outputArg)!;
    var fontSize = parseResult.GetValue(fontSizeOption);
    var font = parseResult.GetValue(fontOption);
    var headingFont = parseResult.GetValue(headingFontOption);
    var orientation = parseResult.GetValue(orientationOption);

    var options = new LayoutOptions
    {
        BodyFontSize = fontSize,
        BodyFontFamily = font ?? new LayoutOptions().BodyFontFamily,
        HeadingFontFamily = headingFont,
        IsLandscape = string.Equals(orientation, "landscape", StringComparison.OrdinalIgnoreCase)
    };

    var converter = new MarkdownToPdfConverter(options);
    converter.ConvertFile(input.FullName, output.FullName);

    Console.WriteLine($"PDF generated: {Path.GetFullPath(output.FullName)}");
});

// --- fonts command ---
var fontsCommand = new Command("fonts", "List supported font families");

fontsCommand.SetAction(_ =>
{
    Console.WriteLine("Supported font families:");
    Console.WriteLine();

    var defaults = new LayoutOptions();
    foreach (var family in SystemFontScanner.GetInstalledFontFamilies())
    {
        var role = family.Equals(defaults.BodyFontFamily, StringComparison.OrdinalIgnoreCase)
            ? " (body default)"
            : family.Equals(defaults.MonoFontFamily, StringComparison.OrdinalIgnoreCase)
                ? " (mono default)"
                : "";
        Console.WriteLine($"  {family}{role}");
    }
});

// --- root command ---
var rootCommand = new RootCommand("Converts Markdown files to PDF")
{
    generateCommand,
    fontsCommand
};

return await rootCommand.Parse(args).InvokeAsync();
