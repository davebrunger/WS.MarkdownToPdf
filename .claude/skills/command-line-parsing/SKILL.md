---
name: command-line-parsing
description: Guidance for parsing command-line input in .NET using the System.CommandLine library — setup patterns, validation, async handlers, DI integration, sub-commands, and testing.
---

# Command-Line Parsing

## Library

Use **`System.CommandLine`** — the official .NET command-line parsing library.

```shell
dotnet add package System.CommandLine --prerelease
```

> The package is currently in preview but is the endorsed path forward and ships in-box from .NET 10.

Reference: <https://learn.microsoft.com/en-us/dotnet/api/system.commandline.parsing.commandlineparser>

---

## Core Concepts

| Concept | Type | Purpose |
|---------|------|---------|
| **RootCommand** | `RootCommand` | Entry point; represents the executable itself |
| **Command** | `Command` | A verb / sub-command (e.g. `convert`, `validate`) |
| **Option** | `Option<T>` | Named parameter (`--output`, `-o`) |
| **Argument** | `Argument<T>` | Positional value |

---

## Setup Pattern

```csharp
using System.CommandLine;

var inputArg  = new Argument<FileInfo>("input", "Markdown file to convert");
var outputOpt = new Option<FileInfo>("--output", "PDF output path") { IsRequired = true };
outputOpt.AddAlias("-o");

var rootCommand = new RootCommand("Converts Markdown files to PDF")
{
    inputArg,
    outputOpt
};

rootCommand.SetAction((parseResult) =>
{
    var input  = parseResult.GetValue(inputArg);
    var output = parseResult.GetValue(outputOpt);

    // Delegate to application code — keep the handler thin
});

return await rootCommand.Parse(args).InvokeAsync();
```

---

## Naming Conventions

- Options use **kebab-case**: `--output-dir`, `--page-size`
- Always provide a **short alias** for frequently used options: `-o`, `-p`
- Arguments use **lowercase nouns**: `input`, `source`

---

## Validation

Prefer built-in validators over manual checks:

```csharp
var input = new Argument<FileInfo>("input");
input.AcceptExistingOnly();           // file must exist

var size = new Option<string>("--size");
size.AcceptOnlyFromAmong("A4", "Letter");
```

For custom validation use `AddValidator`:

```csharp
option.AddValidator(result =>
{
    var value = result.GetValueOrDefault<int>();
    if (value <= 0)
        result.AddError("Value must be positive");
});
```

---

## Async Handlers

Always use async actions when the handler performs I/O:

```csharp
rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var input = parseResult.GetValue(inputArg);
    await converter.ConvertAsync(input!, cancellationToken);
});
```

- Accept and forward `CancellationToken` for graceful shutdown
- Keep handler bodies **thin** — delegate to injected services

---

## Dependency Injection Integration

Wire up the host before invoking the command:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IMarkdownConverter, MarkdownConverter>();

var host = builder.Build();

rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var converter = host.Services.GetRequiredService<IMarkdownConverter>();
    await converter.ConvertAsync(
        parseResult.GetValue(inputArg)!,
        parseResult.GetValue(outputOpt)!,
        cancellationToken);
});

return await rootCommand.Parse(args).InvokeAsync();
```

---

## Sub-Commands

Group related operations under sub-commands:

```csharp
var convertCmd  = new Command("convert", "Convert markdown to PDF") { inputArg, outputOpt };
var validateCmd = new Command("validate", "Check markdown syntax")  { inputArg };

var rootCommand = new RootCommand("Markdown tool")
{
    convertCmd,
    validateCmd
};
```

---

## Testing

### Component Tests (outside-in)

Parse raw `string[]` input and assert behaviour end-to-end:

```csharp
public class CliTests
{
    [Fact]
    public async Task Convert_WithValidArgs_ReturnsZeroExitCode()
    {
        // Arrange
        var args = new[] { "convert", "input.md", "--output", "output.pdf" };

        // Act
        var exitCode = await Program.RunAsync(args);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Theory]
    [InlineData(new[] { "convert" }, "Required argument")]
    [InlineData(new[] { "convert", "missing.md", "-o", "out.pdf" }, "not found")]
    public async Task Convert_WithInvalidArgs_ReturnsNonZero(string[] args, string expectedError)
    {
        var exitCode = await Program.RunAsync(args);
        Assert.NotEqual(0, exitCode);
    }
}
```

### Unit Tests for Validators

```csharp
[Fact]
public void PageSize_RejectsInvalidValue()
{
    var option = BuildPageSizeOption();
    var result = option.Parse("--page-size XXL");

    Assert.NotEmpty(result.Errors);
}
```

---

## Common Pitfalls

| Pitfall | Guidance |
|---------|----------|
| Business logic in handlers | Keep handlers thin; delegate to services |
| Missing cancellation tokens | Always forward `CancellationToken` |
| Forgetting aliases | Add `-x` short forms for common options |
| Hardcoded `Console.Write` | Accept `IConsole` or `TextWriter` for testability |
| Not returning exit codes | Return `int` — `0` for success, non-zero for errors |
