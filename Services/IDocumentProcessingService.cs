using DocuMind.Models;

namespace DocuMind.Services;

/// <summary>
/// Service for extracting text from documents and chunking text into segments.
/// </summary>
public interface IDocumentProcessingService
{
    /// <summary>
    /// Extracts text from PDF or DOCX document while preserving paragraph structure.
    /// </summary>
    /// <param name="documentStream">The document stream to extract text from</param>
    /// <param name="fileExtension">The file extension (.pdf or .docx)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted text content</returns>
    /// <exception cref="ProcessingException">Thrown when text extraction fails or document is empty</exception>
    Task<string> ExtractTextAsync(
        Stream documentStream, 
        string fileExtension, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Chunks text into segments with overlap.
    /// </summary>
    /// <param name="text">The text to chunk</param>
    /// <param name="documentId">The document ID to associate with chunks</param>
    /// <param name="maxTokens">Maximum tokens per chunk (default: 500)</param>
    /// <param name="overlapTokens">Overlap tokens between chunks (default: 50)</param>
    /// <returns>List of chunks with metadata</returns>
    Task<List<DocumentChunk>> ChunkTextAsync(
        string text, 
        string documentId, 
        int maxTokens = 500, 
        int overlapTokens = 50);
}
