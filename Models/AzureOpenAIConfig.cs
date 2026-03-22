namespace DocuMind.Models;

/// <summary>
/// Configuration model for Azure OpenAI service settings.
/// </summary>
public class AzureOpenAIConfig
{
    /// <summary>
    /// The Azure OpenAI service endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// The API key for authenticating with Azure OpenAI.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The deployment name for the GPT model (e.g., gpt-4o).
    /// </summary>
    public string GptDeployment { get; set; } = string.Empty;

    /// <summary>
    /// The deployment name for the embedding model (e.g., text-embedding-3-small).
    /// </summary>
    public string EmbeddingDeployment { get; set; } = string.Empty;

    /// <summary>
    /// Validates that all required configuration values are present.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when any required value is missing.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException("AzureOpenAI:Endpoint configuration is missing or empty.");
        
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("AzureOpenAI:Key configuration is missing or empty.");
        
        if (string.IsNullOrWhiteSpace(GptDeployment))
            throw new InvalidOperationException("AzureOpenAI:GptDeployment configuration is missing or empty.");
        
        if (string.IsNullOrWhiteSpace(EmbeddingDeployment))
            throw new InvalidOperationException("AzureOpenAI:EmbeddingDeployment configuration is missing or empty.");
    }
}
