using Microsoft.AspNetCore.Mvc;
using DocuMind.Services;
using DocuMind.Models;
using DocuMind.Exceptions;
using DocuMind.Plugins;
using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Text;

namespace DocuMind.Controllers;

/// <summary>
/// Controller for document upload and query operations.
/// </summary>
[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly IDocumentProcessingService _documentProcessingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ISearchService _searchService;
    private readonly ILogger<DocumentsController> _logger;
    private readonly Kernel _kernel;
    private readonly SearchPlugin _searchPlugin;

    public DocumentsController(
        IBlobStorageService blobStorageService,
        IDocumentProcessingService documentProcessingService,
        IEmbeddingService embeddingService,
        ISearchService searchService,
        ILogger<DocumentsController> logger,
        Kernel kernel,
        SearchPlugin searchPlugin)
    {
        _blobStorageService = blobStorageService;
        _documentProcessingService = documentProcessingService;
        _embeddingService = embeddingService;
        _searchService = searchService;
        _logger = logger;
        _kernel = kernel;
        _searchPlugin = searchPlugin;
    }

    /// <summary>
    /// Uploads a PDF or DOCX document for processing and indexing.
    /// </summary>
    /// <param name="file">The document file to upload (PDF or DOCX only)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload response with documentId, chunkCount, and fileName</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument(IFormFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file is provided
            if (file == null || file.Length == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "ValidationError",
                    Message = "No file provided or file is empty."
                });
            }

            // Validate file format (PDF or DOCX only)
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (fileExtension != ".pdf" && fileExtension != ".docx")
            {
                return BadRequest(new ErrorResponse
                {
                    Error = "ValidationError",
                    Message = "File format not supported. Only PDF and DOCX files are accepted."
                });
            }

            string documentId;
            string blobUri;
            List<DocumentChunk> chunks;
            
            // Upload file to blob storage
            using (var stream = file.OpenReadStream())
            {
                (documentId, blobUri) = await _blobStorageService.UploadDocumentAsync(
                    stream, 
                    file.FileName, 
                    cancellationToken);
            }

            // Extract text from document
            string extractedText;
            using (var stream = file.OpenReadStream())
            {
                extractedText = await _documentProcessingService.ExtractTextAsync(
                    stream, 
                    fileExtension, 
                    cancellationToken);
            }

            // Chunk the text
            chunks = await _documentProcessingService.ChunkTextAsync(
                extractedText, 
                documentId, 
                maxTokens: 500, 
                overlapTokens: 50);

            // Generate embeddings for all chunks in batch
            var chunkTexts = chunks.Select(c => c.Text).ToList();
            var embeddings = await _embeddingService.GenerateEmbeddingsBatchAsync(
                chunkTexts, 
                cancellationToken);

            // Index chunks with embeddings
            await _searchService.IndexChunksAsync(
                chunks, 
                embeddings, 
                cancellationToken);

            // Log successful upload
            _logger.LogInformation(
                "Document uploaded successfully. DocumentId: {DocumentId}, FileName: {FileName}, ChunkCount: {ChunkCount}",
                documentId, file.FileName, chunks.Count);

            // Return response
            return Ok(new UploadResponse
            {
                DocumentId = documentId,
                ChunkCount = chunks.Count,
                FileName = file.FileName
            });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error during document upload: {Message}", ex.Message);
            return BadRequest(new ErrorResponse
            {
                Error = "ValidationError",
                Message = ex.Message
            });
        }
        catch (ServiceUnavailableException ex)
        {
            _logger.LogError(ex, "Service unavailable during document upload: {Message}", ex.Message);
            return StatusCode(503, new ErrorResponse
            {
                Error = "ServiceUnavailable",
                Message = ex.Message,
                RetryGuidance = "Please retry your request after 30 seconds. If the issue persists, contact support."
            });
        }
        catch (ProcessingException ex)
        {
            _logger.LogError(ex, "Processing error during document upload: {Message}", ex.Message);
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalServerError",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during document upload");
            return StatusCode(500, new ErrorResponse
            {
                Error = "InternalServerError",
                Message = "An unexpected error occurred while processing your document."
            });
        }
    }

    /// <summary>
    /// Queries documents with a natural language question and streams the response via Server-Sent Events.
    /// </summary>
    /// <param name="request">Query request containing the question and optional documentId filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Server-Sent Events stream with the response</returns>
    [HttpPost("query")]
    public async Task QueryDocument([FromBody] QueryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
            {
                Response.StatusCode = 400;
                Response.ContentType = "application/json";
                await Response.WriteAsJsonAsync(new ErrorResponse
                {
                    Error = "ValidationError",
                    Message = "Question is required and cannot be empty."
                }, cancellationToken);
                return;
            }

            // Set up Server-Sent Events
            Response.ContentType = "text/event-stream";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            // Log query operation
            _logger.LogInformation(
                "Query received. Question: {Question}, DocumentId: {DocumentId}",
                request.Question, request.DocumentId ?? "none");

            // Task 10.4: Query orchestration with Semantic Kernel
            
            // Step 1: Generate query embedding
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(
                request.Question, 
                cancellationToken);

            // Step 2: Perform vector search with optional documentId filter
            var searchResults = await _searchService.SearchAsync(
                queryEmbedding,
                topN: 5,
                documentIdFilter: request.DocumentId,
                cancellationToken: cancellationToken);

            // Check if we found any relevant chunks
            if (searchResults.Count == 0)
            {
                await Response.WriteAsync("event: token\n", cancellationToken);
                await Response.WriteAsync("data: {\"content\": \"I couldn't find any relevant information to answer your question.\"}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                await Response.WriteAsync("event: complete\n", cancellationToken);
                await Response.WriteAsync("data: {\"status\": \"completed\"}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
                return;
            }

            // Step 3: Build context from search results
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("Use the following document chunks to answer the question:");
            contextBuilder.AppendLine();
            
            for (int i = 0; i < searchResults.Count; i++)
            {
                var result = searchResults[i];
                contextBuilder.AppendLine($"[Chunk {i + 1}]");
                contextBuilder.AppendLine($"Document ID: {result.Chunk.DocumentId}");
                contextBuilder.AppendLine($"Chunk ID: {result.Chunk.ChunkId}");
                contextBuilder.AppendLine($"Content: {result.Chunk.Text}");
                contextBuilder.AppendLine();
            }

            contextBuilder.AppendLine($"Question: {request.Question}");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Please provide a comprehensive answer based on the document chunks above. If the chunks don't contain enough information to fully answer the question, acknowledge this in your response.");

            var prompt = contextBuilder.ToString();

            // Step 4: Invoke gpt-4o via Semantic Kernel with streaming
            // Task 10.5: SSE streaming logic
            
            var executionSettings = new Microsoft.SemanticKernel.PromptExecutionSettings
            {
                ExtensionData = new Dictionary<string, object>
                {
                    ["max_tokens"] = 1000,
                    ["temperature"] = 0.7
                }
            };

            // Stream the response from gpt-4o
            await foreach (var streamChunk in _kernel.InvokePromptStreamingAsync(prompt, new(executionSettings), cancellationToken: cancellationToken))
            {
                var content = streamChunk.ToString();
                if (!string.IsNullOrEmpty(content))
                {
                    // Send token event
                    var tokenData = JsonSerializer.Serialize(new { content });
                    await Response.WriteAsync("event: token\n", cancellationToken);
                    await Response.WriteAsync($"data: {tokenData}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }

            // Task 10.5: Send citations for source chunks
            foreach (var result in searchResults)
            {
                var citationData = JsonSerializer.Serialize(new
                {
                    chunkId = result.Chunk.ChunkId,
                    documentId = result.Chunk.DocumentId,
                    score = Math.Round(result.SimilarityScore, 4)
                });
                await Response.WriteAsync("event: citation\n", cancellationToken);
                await Response.WriteAsync($"data: {citationData}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            // Send completion event
            await Response.WriteAsync("event: complete\n", cancellationToken);
            await Response.WriteAsync("data: {\"status\": \"completed\"}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            _logger.LogInformation(
                "Query completed successfully. Question: {Question}, ChunksRetrieved: {ChunkCount}",
                request.Question, searchResults.Count);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error during query: {Message}", ex.Message);
            await Response.WriteAsync("event: error\n", cancellationToken);
            var errorData = JsonSerializer.Serialize(new { error = "ValidationError", message = ex.Message });
            await Response.WriteAsync($"data: {errorData}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (ServiceUnavailableException ex)
        {
            _logger.LogError(ex, "Service unavailable during query: {Message}", ex.Message);
            await Response.WriteAsync("event: error\n", cancellationToken);
            var errorData = JsonSerializer.Serialize(new { error = "ServiceUnavailable", message = ex.Message });
            await Response.WriteAsync($"data: {errorData}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - log and clean up
            _logger.LogInformation("Client disconnected during query streaming");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during query. Question: {Question}", request?.Question ?? "unknown");
            try
            {
                await Response.WriteAsync("event: error\n", cancellationToken);
                await Response.WriteAsync("data: {\"error\": \"InternalServerError\", \"message\": \"An error occurred while processing your query.\"}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            catch
            {
                // If we can't write the error, the client has likely disconnected
                _logger.LogWarning("Failed to send error event to client - connection may be closed");
            }
        }
    }
}
