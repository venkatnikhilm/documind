namespace DocuMind.Models;

/// <summary>
/// Configuration model for Azure Blob Storage service settings.
/// </summary>
public class AzureBlobConfig
{
    /// <summary>
    /// The connection string for Azure Blob Storage.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The name of the blob container for storing documents.
    /// </summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Validates that all required configuration values are present.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when any required value is missing.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("AzureBlob:ConnectionString configuration is missing or empty.");
        
        if (string.IsNullOrWhiteSpace(Container))
            throw new InvalidOperationException("AzureBlob:Container configuration is missing or empty.");
    }
}
