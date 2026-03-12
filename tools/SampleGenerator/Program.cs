using System.CommandLine;
using WS.MarkdownToPdf;
using WS.MarkdownToPdf.Layout;

var inputArg = new Argument<FileInfo>("input") { Description = "Markdown file to convert", DefaultValueFactory = _ => new FileInfo("sample.md") };
var outputArg = new Argument<FileInfo>("output") { Description = "PDF output path", DefaultValueFactory = _ => new FileInfo("sample.pdf") };
var fontSizeOption = new Option<double>("--font-size", "-f") { Description = "Body font size in points", DefaultValueFactory = _ => 10 };

var rootCommand = new RootCommand("Converts Markdown files to PDF")
{
    inputArg,
    outputArg,
    fontSizeOption
};

rootCommand.SetAction(parseResult =>
{
    var input = parseResult.GetValue(inputArg)!;
    var output = parseResult.GetValue(outputArg)!;
    var fontSize = parseResult.GetValue(fontSizeOption);

    var options = new LayoutOptions { BodyFontSize = fontSize };
    var converter = new MarkdownToPdfConverter(options);
    converter.ConvertFile(input.FullName, output.FullName);

    Console.WriteLine($"PDF generated: {Path.GetFullPath(output.FullName)}");
});

return await rootCommand.Parse(args).InvokeAsync();
