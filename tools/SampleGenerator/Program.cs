using WS.MarkdownToPdf;

var inputPath = args.Length > 0 ? args[0] : "sample.md";
var outputPath = args.Length > 1 ? args[1] : "sample.pdf";

var converter = new MarkdownToPdfConverter();
converter.ConvertFile(inputPath, outputPath);

Console.WriteLine($"PDF generated: {Path.GetFullPath(outputPath)}");
