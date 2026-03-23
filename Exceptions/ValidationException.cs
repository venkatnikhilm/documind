namespace DocuMind.Exceptions;

/// <summary>
/// Exception thrown when request validation fails.
/// Maps to HTTP 400 Bad Request responses.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
