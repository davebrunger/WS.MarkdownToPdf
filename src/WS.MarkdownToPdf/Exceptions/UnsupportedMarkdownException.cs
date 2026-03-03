namespace WS.MarkdownToPdf.Exceptions;

/// <summary>
/// Thrown when the renderer encounters a Markdown AST node type it does not support.
/// </summary>
public class UnsupportedMarkdownException : Exception
{
    /// <summary>
    /// The 1-based line number where the unsupported element begins.
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The 1-based column number where the unsupported element begins.
    /// </summary>
    public int Column { get; }

    public UnsupportedMarkdownException(string nodeType, int line, int column)
        : base($"Unsupported Markdown element: {nodeType} at line {line}, column {column}")
    {
        Line = line;
        Column = column;
    }

    public UnsupportedMarkdownException(string nodeType, int line, int column, Exception innerException)
        : base($"Unsupported Markdown element: {nodeType} at line {line}, column {column}", innerException)
    {
        Line = line;
        Column = column;
    }
}
