namespace DocuMind.Models;

public class QueryRequest
{
    public string Question { get; set; } = string.Empty;
    public string? DocumentId { get; set; }
}
