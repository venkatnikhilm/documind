namespace DocuMind.Models;

/// <summary>
/// Configuration model for Azure AI Search service settings.
/// </summary>
public class AzureSearchConfig
{
    /// <summary>
    /// The Azure AI Search service endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// The API key for authenticating with Azure AI Search.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The name of the search index for document chunks.
    /// </summary>
    public string IndexName { get; set; } = string.Empty;

    /// <summary>
    /// Validates that all required configuration values are present.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when any required value is missing.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Endpoint))
            throw new InvalidOperationException("AzureSearch:Endpoint configuration is missing or empty.");
        
        if (string.IsNullOrWhiteSpace(Key))
            throw new InvalidOperationException("AzureSearch:Key configuration is missing or empty.");
        
        if (string.IsNullOrWhiteSpace(IndexName))
            throw new InvalidOperationException("AzureSearch:IndexName configuration is missing or empty.");
    }
}
