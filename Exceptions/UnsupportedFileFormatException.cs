namespace DocuMind.Exceptions;

/// <summary>
/// Exception thrown when an unsupported file format is provided.
/// This is a specialized validation exception for file format errors.
/// Maps to HTTP 400 Bad Request responses.
/// </summary>
public class UnsupportedFileFormatException : ValidationException
{
    public UnsupportedFileFormatException(string message) : base(message)
    {
    }

    public UnsupportedFileFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
