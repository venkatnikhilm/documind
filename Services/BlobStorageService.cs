using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DocuMind.Models;

namespace DocuMind.Services;

/// <summary>
/// Service for managing document storage in Azure Blob Storage.
/// Implements requirements 1.2, 8.1, 8.2, 8.3, 8.4, 8.5.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(AzureBlobConfig config, ILogger<BlobStorageService> logger)
    {
        _logger = logger;

        try
        {
            var blobServiceClient = new BlobServiceClient(config.ConnectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(config.Container);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize BlobStorageService with container: {Container}", config.Container);
            throw;
        }
    }

    /// <summary>
    /// Uploads a document to Azure Blob Storage with a unique GUID-based name.
    /// Preserves the original file extension.
    /// </summary>
    public async Task<(string DocumentId, string BlobUri)> UploadDocumentAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate unique documentId (GUID) - Requirement 8.2
            var documentId = Guid.NewGuid().ToString();

            // Preserve file extension - Requirement 8.4
            var fileExtension = Path.GetExtension(fileName);
            var blobName = $"{documentId}{fileExtension}";

            // Get blob client for the new blob
            var blobClient = _containerClient.GetBlobClient(blobName);

            // Upload the file stream to blob storage - Requirements 1.2, 8.1
            await blobClient.UploadAsync(
                fileStream,
                new BlobHttpHeaders { ContentType = GetContentType(fileExtension) },
                cancellationToken: cancellationToken);

            // Return documentId and blob URI - Requirement 8.3
            var blobUri = blobClient.Uri.ToString();

            _logger.LogInformation(
                "Document uploaded to blob storage. DocumentId: {DocumentId}, BlobName: {BlobName}, BlobUri: {BlobUri}",
                documentId, blobName, blobUri);

            return (documentId, blobUri);
        }
        catch (RequestFailedException ex) when (ex.Status == 503 || ex.Status == 500)
        {
            // Handle service unavailability - Requirement 8.5
            _logger.LogError(ex, "Azure Blob Storage service is unavailable. Status: {Status}", ex.Status);
            throw new ServiceUnavailableException("Azure Blob Storage service is currently unavailable.", ex);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to upload document to blob storage. Status: {Status}, ErrorCode: {ErrorCode}",
                ex.Status, ex.ErrorCode);
            throw new InvalidOperationException($"Failed to upload document to blob storage: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during document upload to blob storage");
            throw;
        }
    }

    /// <summary>
    /// Gets the appropriate content type based on file extension.
    /// </summary>
    private static string GetContentType(string fileExtension)
    {
        return fileExtension.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}
