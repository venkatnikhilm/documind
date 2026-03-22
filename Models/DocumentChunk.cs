namespace DocuMind.Models;

public class DocumentChunk
{
    public string ChunkId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int StartPosition { get; set; }
    public int EndPosition { get; set; }
}
