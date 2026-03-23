namespace DocuMind.Models;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? RetryGuidance { get; set; }
}
