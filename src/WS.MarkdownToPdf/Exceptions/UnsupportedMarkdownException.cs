namespace WS.MarkdownToPdf.Exceptions;

/// <summary>
/// Thrown when the renderer encounters a Markdown AST node type it does not support.
/// </summary>
public class UnsupportedMarkdownException : Exception
{
    public UnsupportedMarkdownException(string nodeType)
        : base($"Unsupported Markdown element: {nodeType}")
    {
    }

    public UnsupportedMarkdownException(string nodeType, Exception innerException)
        : base($"Unsupported Markdown element: {nodeType}", innerException)
    {
    }
}
