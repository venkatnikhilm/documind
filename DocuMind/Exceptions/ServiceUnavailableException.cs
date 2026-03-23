namespace DocuMind.Exceptions;

/// <summary>
/// Exception thrown when an external Azure service is unavailable.
/// Maps to HTTP 503 Service Unavailable responses.
/// </summary>
public class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string message) : base(message)
    {
    }

    public ServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
