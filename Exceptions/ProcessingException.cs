namespace DocuMind.Exceptions;

/// <summary>
/// Exception thrown when document processing fails.
/// Maps to HTTP 500 Internal Server Error responses.
/// </summary>
public class ProcessingException : Exception
{
    public ProcessingException(string message) : base(message)
    {
    }

    public ProcessingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
