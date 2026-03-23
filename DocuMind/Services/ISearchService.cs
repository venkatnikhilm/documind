namespace DocuMind.Services;

using DocuMind.Models;

/// <summary>
/// Service interface for Azure AI Search operations including indexing and vector similarity search.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Indexes document chunks with their embeddings into Azure AI Search.
    /// </summary>
    /// <param name="chunks">The document chunks to index.</param>
    /// <param name="embeddings">The embedding vectors corresponding to each chunk.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task IndexChunksAsync(
        List<DocumentChunk> chunks, 
        List<float[]> embeddings, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs vector similarity search to find the most relevant document chunks.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding vector.</param>
    /// <param name="topN">The maximum number of results to return (default: 5).</param>
    /// <param name="documentIdFilter">Optional document ID to filter results to a specific document.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A list of search results ordered by similarity score descending.</returns>
    Task<List<SearchResult>> SearchAsync(
        float[] queryEmbedding, 
        int topN = 5, 
        string? documentIdFilter = null, 
        CancellationToken cancellationToken = default);
}
