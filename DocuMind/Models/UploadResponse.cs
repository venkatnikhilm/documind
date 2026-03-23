namespace DocuMind.Models;

public class UploadResponse
{
    public string DocumentId { get; set; } = string.Empty;
    public int ChunkCount { get; set; }
    public string FileName { get; set; } = string.Empty;
}
