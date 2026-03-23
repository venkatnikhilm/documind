namespace DocuMind.Plugins;

using System.ComponentModel;
using System.Text;
using DocuMind.Services;
using Microsoft.SemanticKernel;

/// <summary>
/// Semantic Kernel plugin that performs vector search operations on document chunks.
/// </summary>
public class SearchPlugin
{
    private readonly ISearchService _searchService;
    private readonly IEmbeddingService _embeddingService;

    /// <summary>
    /// Initializes a new instance of the SearchPlugin class.
    /// </summary>
    /// <param name="searchService">The search service for performing vector similarity search.</param>
    /// <param name="embeddingService">The embedding service for generating query embeddings.</param>
    public SearchPlugin(ISearchService searchService, IEmbeddingService embeddingService)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
    }

    /// <summary>
    /// Searches for relevant document chunks based on a natural language query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="documentId">Optional document ID to filter results to a specific document.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A formatted string containing the search results with chunk text and metadata.</returns>
    [KernelFunction("search_documents")]
    [Description("Searches for relevant document chunks based on a query")]
    public async Task<string> SearchDocumentsAsync(
        [Description("The search query")] string query,
        [Description("Optional document ID to filter results")] string? documentId = null,
        CancellationToken cancellationToken = default)
    {
        // Generate embedding for the query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);

        // Perform vector similarity search
        var searchResults = await _searchService.SearchAsync(
            queryEmbedding, 
            topN: 5, 
            documentIdFilter: documentId, 
            cancellationToken: cancellationToken);

        // Format search results as a string for the kernel
        if (searchResults.Count == 0)
        {
            return "No relevant document chunks found.";
        }

        var resultBuilder = new StringBuilder();
        resultBuilder.AppendLine("Search Results:");
        resultBuilder.AppendLine();

        for (int i = 0; i < searchResults.Count; i++)
        {
            var result = searchResults[i];
            resultBuilder.AppendLine($"Result {i + 1} (Score: {result.SimilarityScore:F4}):");
            resultBuilder.AppendLine($"Document ID: {result.Chunk.DocumentId}");
            resultBuilder.AppendLine($"Chunk ID: {result.Chunk.ChunkId}");
            resultBuilder.AppendLine($"Chunk Index: {result.Chunk.ChunkIndex}");
            resultBuilder.AppendLine($"Text: {result.Chunk.Text}");
            resultBuilder.AppendLine();
        }

        return resultBuilder.ToString();
    }
}
