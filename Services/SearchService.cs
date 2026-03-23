using DocuMind.Exceptions;
namespace DocuMind.Services;

using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using DocuMind.Exceptions;
using DocuMind.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for Azure AI Search operations including index management and vector similarity search.
/// </summary>
public class SearchService : ISearchService
{
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _indexClient;
    private readonly ILogger<SearchService> _logger;
    private readonly string _indexName;

    public SearchService(
        AzureSearchConfig config,
        ILogger<SearchService> logger)
    {
        _logger = logger;
        _indexName = config.IndexName;

        var credential = new AzureKeyCredential(config.Key);
        _indexClient = new SearchIndexClient(new Uri(config.Endpoint), credential);
        _searchClient = _indexClient.GetSearchClient(_indexName);

        // Create index on initialization if it doesn't exist
        InitializeIndexAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates the Azure AI Search index with vector search configuration if it doesn't exist.
    /// </summary>
    private async Task InitializeIndexAsync()
    {
        try
        {
            // Check if index already exists
            try
            {
                await _indexClient.GetIndexAsync(_indexName);
                _logger.LogInformation("Search index '{IndexName}' already exists.", _indexName);
                return;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Index doesn't exist, create it
                _logger.LogInformation("Creating search index '{IndexName}'...", _indexName);
            }

            // Define the index schema
            var index = new SearchIndex(_indexName)
            {
                Fields =
                {
                    new SimpleField("chunkId", SearchFieldDataType.String) 
                    { 
                        IsKey = true, 
                        IsFilterable = false, 
                        IsSortable = false, 
                        IsFacetable = false 
                    },
                    new SimpleField("documentId", SearchFieldDataType.String) 
                    { 
                        IsFilterable = true, 
                        IsSortable = false, 
                        IsFacetable = false 
                    },
                    new SearchableField("text") 
                    { 
                        IsFilterable = false, 
                        IsSortable = false 
                    },
                    new SearchField("embedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                    {
                        IsSearchable = true,
                        VectorSearchDimensions = 1536,
                        VectorSearchProfileName = "cosine-profile"
                    },
                    new SimpleField("chunkIndex", SearchFieldDataType.Int32) 
                    { 
                        IsFilterable = true, 
                        IsSortable = true, 
                        IsFacetable = false 
                    },
                    new SimpleField("startPosition", SearchFieldDataType.Int32) 
                    { 
                        IsFilterable = false, 
                        IsSortable = false, 
                        IsFacetable = false 
                    },
                    new SimpleField("endPosition", SearchFieldDataType.Int32) 
                    { 
                        IsFilterable = false, 
                        IsSortable = false, 
                        IsFacetable = false 
                    }
                }
            };

            // Configure vector search with HNSW algorithm and cosine similarity
            index.VectorSearch = new VectorSearch
            {
                Profiles =
                {
                    new VectorSearchProfile("cosine-profile", "hnsw-config")
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration("hnsw-config")
                    {
                        Parameters = new HnswParameters
                        {
                            Metric = VectorSearchAlgorithmMetric.Cosine,
                            M = 4,
                            EfConstruction = 400,
                            EfSearch = 500
                        }
                    }
                }
            };

            // Create the index
            await _indexClient.CreateIndexAsync(index);
            _logger.LogInformation("Successfully created search index '{IndexName}'.", _indexName);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to initialize search index '{IndexName}'.", _indexName);
            throw new ServiceUnavailableException("Azure AI Search service is unavailable.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during search index initialization.");
            throw new ProcessingException("Failed to initialize search index.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task IndexChunksAsync(
        List<DocumentChunk> chunks, 
        List<float[]> embeddings, 
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count != embeddings.Count)
        {
            throw new ArgumentException("The number of chunks must match the number of embeddings.");
        }

        try
        {
            // Create search documents with all required fields
            var documents = chunks.Select((chunk, index) => new SearchDocument
            {
                ["chunkId"] = chunk.ChunkId,
                ["documentId"] = chunk.DocumentId,
                ["text"] = chunk.Text,
                ["embedding"] = embeddings[index],
                ["chunkIndex"] = chunk.ChunkIndex,
                ["startPosition"] = chunk.StartPosition,
                ["endPosition"] = chunk.EndPosition
            }).ToList();

            // Upload documents to the index
            var batch = IndexDocumentsBatch.Upload(documents);
            var result = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Successfully indexed {ChunkCount} chunks for document {DocumentId}.",
                chunks.Count,
                chunks.FirstOrDefault()?.DocumentId ?? "unknown");
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to index chunks. Status: {Status}", ex.Status);
            throw new ServiceUnavailableException("Azure AI Search service is unavailable.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during chunk indexing.");
            throw new ProcessingException("Failed to index document chunks.", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<List<SearchResult>> SearchAsync(
        float[] queryEmbedding, 
        int topN = 5, 
        string? documentIdFilter = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Create vector query for similarity search
            var vectorQuery = new VectorizedQuery(queryEmbedding)
            {
                KNearestNeighborsCount = topN,
                Fields = { "embedding" }
            };

            // Configure search options
            var searchOptions = new SearchOptions
            {
                VectorSearch = new VectorSearchOptions
                {
                    Queries = { vectorQuery }
                },
                Size = topN,
                Select = { "chunkId", "documentId", "text", "chunkIndex", "startPosition", "endPosition" }
            };

            // Add document filter if provided
            if (!string.IsNullOrWhiteSpace(documentIdFilter))
            {
                searchOptions.Filter = $"documentId eq '{documentIdFilter}'";
            }

            // Perform vector search with cosine similarity (configured in index)
            var response = await _searchClient.SearchAsync<SearchDocument>(
                null,
                searchOptions,
                cancellationToken: cancellationToken);

            // Process results - Azure AI Search returns results ordered by similarity score descending
            var results = new List<SearchResult>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                var chunk = new DocumentChunk
                {
                    ChunkId = result.Document["chunkId"]?.ToString() ?? string.Empty,
                    DocumentId = result.Document["documentId"]?.ToString() ?? string.Empty,
                    Text = result.Document["text"]?.ToString() ?? string.Empty,
                    ChunkIndex = Convert.ToInt32(result.Document["chunkIndex"]),
                    StartPosition = Convert.ToInt32(result.Document["startPosition"]),
                    EndPosition = Convert.ToInt32(result.Document["endPosition"])
                };

                results.Add(new SearchResult
                {
                    Chunk = chunk,
                    SimilarityScore = result.Score ?? 0.0
                });
            }

            _logger.LogInformation(
                "Vector search completed. Query returned {ResultCount} results. Filter: {Filter}",
                results.Count,
                documentIdFilter ?? "none");

            // Results are already ordered by similarity score descending from Azure AI Search
            return results;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to perform vector search. Status: {Status}", ex.Status);
            throw new ServiceUnavailableException("Azure AI Search service is unavailable.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during vector search.");
            throw new ProcessingException("Failed to perform vector search.", ex);
        }
    }
}
