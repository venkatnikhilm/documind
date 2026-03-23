namespace DocuMind.Services;

/// <summary>
/// Service interface for generating vector embeddings using Azure OpenAI.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates an embedding vector for a single text string.
    /// </summary>
    /// <param name="text">The text to generate an embedding for.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A float array representing the embedding vector.</returns>
    /// <exception cref="ServiceUnavailableException">Thrown when Azure OpenAI service is unavailable.</exception>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embedding vectors for multiple text strings in batch.
    /// </summary>
    /// <param name="texts">The list of texts to generate embeddings for.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A list of float arrays representing the embedding vectors.</returns>
    /// <exception cref="ServiceUnavailableException">Thrown when Azure OpenAI service is unavailable.</exception>
    Task<List<float[]>> GenerateEmbeddingsBatchAsync(List<string> texts, CancellationToken cancellationToken = default);
}
