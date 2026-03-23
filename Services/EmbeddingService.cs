using DocuMind.Exceptions;
using Azure;
using Azure.AI.OpenAI;
using DocuMind.Models;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Tokenizers;
using OpenAI.Embeddings;
using Polly;
using Polly.Retry;

namespace DocuMind.Services;

/// <summary>
/// Service for generating vector embeddings using Azure OpenAI text-embedding-3-small deployment.
/// Implements retry logic with exponential backoff and token limit handling.
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _embeddingDeployment;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly Tokenizer _tokenizer;
    private const int MaxTokens = 8191; // text-embedding-3-small token limit

    public EmbeddingService(
        AzureOpenAIConfig config,
        ILogger<EmbeddingService> logger)
    {
        _client = new AzureOpenAIClient(
            new Uri(config.Endpoint),
            new AzureKeyCredential(config.Key));
        
        _embeddingDeployment = config.EmbeddingDeployment;
        _logger = logger;

        // Configure retry policy with exponential backoff (3 retries)
        _retryPolicy = Policy
            .Handle<RequestFailedException>(ex => IsTransientError(ex))
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Retry {RetryCount} after {Delay}s due to transient error",
                        retryCount,
                        timeSpan.TotalSeconds);
                });

        // Initialize tokenizer for token counting
        _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
    }

    /// <inheritdoc/>
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            // Truncate text if it exceeds token limit
            var processedText = TruncateIfNeeded(text);

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var embeddingClient = _client.GetEmbeddingClient(_embeddingDeployment);
                var options = new EmbeddingGenerationOptions();
                var response = await embeddingClient.GenerateEmbeddingAsync(processedText, options, cancellationToken);
                
                return response.Value.ToFloats().ToArray();
            });
        }
        catch (RequestFailedException ex) when (!IsTransientError(ex))
        {
            _logger.LogError(
                ex,
                "Azure OpenAI service unavailable. Operation: GenerateEmbedding, TextLength: {Length}",
                text.Length);
            
            throw new ServiceUnavailableException(
                "Azure OpenAI service is currently unavailable. Please retry your request after 30 seconds.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error generating embedding. Operation: GenerateEmbedding, TextLength: {Length}",
                text.Length);
            
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<float[]>> GenerateEmbeddingsBatchAsync(
        List<string> texts,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Truncate texts if needed
            var processedTexts = texts.Select(TruncateIfNeeded).ToList();

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var embeddingClient = _client.GetEmbeddingClient(_embeddingDeployment);
                var options = new EmbeddingGenerationOptions();
                var response = await embeddingClient.GenerateEmbeddingsAsync(processedTexts, options, cancellationToken);
                
                return response.Value
                    .Select(embedding => embedding.ToFloats().ToArray())
                    .ToList();
            });
        }
        catch (RequestFailedException ex) when (!IsTransientError(ex))
        {
            _logger.LogError(
                ex,
                "Azure OpenAI service unavailable. Operation: GenerateEmbeddingsBatch, TextCount: {Count}",
                texts.Count);
            
            throw new ServiceUnavailableException(
                "Azure OpenAI service is currently unavailable. Please retry your request after 30 seconds.",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error generating embeddings batch. Operation: GenerateEmbeddingsBatch, TextCount: {Count}",
                texts.Count);
            
            throw;
        }
    }

    /// <summary>
    /// Truncates text if it exceeds the token limit and logs a warning.
    /// </summary>
    private string TruncateIfNeeded(string text)
    {
        var tokenCount = _tokenizer.CountTokens(text);
        
        if (tokenCount <= MaxTokens)
        {
            return text;
        }

        _logger.LogWarning(
            "Text exceeds token limit. TokenCount: {TokenCount}, MaxTokens: {MaxTokens}. Truncating text.",
            tokenCount,
            MaxTokens);

        // Encode and truncate to max tokens
        var tokens = _tokenizer.EncodeToIds(text);
        var truncatedTokens = tokens.Take(MaxTokens);
        var truncatedText = _tokenizer.Decode(truncatedTokens.ToArray());

        return truncatedText ?? string.Empty;
    }

    /// <summary>
    /// Determines if an exception represents a transient error that should be retried.
    /// </summary>
    private static bool IsTransientError(RequestFailedException ex)
    {
        // Retry on rate limiting (429) and server errors (5xx)
        return ex.Status == 429 || (ex.Status >= 500 && ex.Status < 600);
    }
}
