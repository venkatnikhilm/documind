namespace DocuMind.Services;

/// <summary>
/// Service interface for managing document storage in Azure Blob Storage.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a raw document to Azure Blob Storage.
    /// </summary>
    /// <param name="fileStream">The file stream to upload.</param>
    /// <param name="fileName">The original file name (used to preserve extension).</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A tuple containing the generated documentId (GUID) and the blob URI.</returns>
    /// <exception cref="ServiceUnavailableException">Thrown when Azure Blob Storage service is unavailable.</exception>
    Task<(string DocumentId, string BlobUri)> UploadDocumentAsync(
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
