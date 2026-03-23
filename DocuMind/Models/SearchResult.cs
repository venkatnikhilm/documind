namespace DocuMind.Models;

public class SearchResult
{
    public DocumentChunk Chunk { get; set; } = new();
    public double SimilarityScore { get; set; }
}
