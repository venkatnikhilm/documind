using DocuMind.Exceptions;
using DocuMind.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Tokenizers;
using System.Text;

namespace DocuMind.Services;

/// <summary>
/// Service for extracting text from documents and chunking text into segments.
/// </summary>
public class DocumentProcessingService : IDocumentProcessingService
{
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(ILogger<DocumentProcessingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts text from PDF or DOCX document while preserving paragraph structure.
    /// </summary>
    public async Task<string> ExtractTextAsync(
        Stream documentStream, 
        string fileExtension, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            string extractedText = fileExtension.ToLowerInvariant() switch
            {
                ".pdf" => await ExtractTextFromPdfAsync(documentStream, cancellationToken),
                ".docx" => await ExtractTextFromDocxAsync(documentStream, cancellationToken),
                _ => throw new ProcessingException($"Unsupported file format: {fileExtension}")
            };

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                throw new ProcessingException("Document contains no extractable text.");
            }

            _logger.LogInformation(
                "Successfully extracted text from {FileExtension} document. Length: {Length} characters",
                fileExtension, extractedText.Length);

            return extractedText;
        }
        catch (ProcessingException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during text extraction for {FileExtension}", fileExtension);
            throw new ProcessingException($"Failed to extract text from {fileExtension} document.", ex);
        }
    }

    /// <summary>
    /// Chunks text into segments with overlap.
    /// </summary>
    public Task<List<DocumentChunk>> ChunkTextAsync(
        string text, 
        string documentId, 
        int maxTokens = 500, 
        int overlapTokens = 50)
    {
        try
        {
            // Handle empty or null text
            if (string.IsNullOrWhiteSpace(text))
            {
                return Task.FromResult(new List<DocumentChunk>());
            }

            // Create tokenizer using cl100k_base encoding (used by text-embedding-3-small)
            var tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
            
            // Encode the entire text to get token IDs
            var allTokenIds = tokenizer.EncodeToIds(text);
            
            // If text is shorter than maxTokens, return single chunk
            if (allTokenIds.Count <= maxTokens)
            {
                var chunk = new DocumentChunk
                {
                    ChunkId = Guid.NewGuid().ToString(),
                    DocumentId = documentId,
                    Text = text,
                    ChunkIndex = 0,
                    StartPosition = 0,
                    EndPosition = text.Length
                };
                
                _logger.LogInformation(
                    "Text chunked into 1 chunk for document {DocumentId}. Total tokens: {TokenCount}",
                    documentId, allTokenIds.Count);
                
                return Task.FromResult(new List<DocumentChunk> { chunk });
            }

            // Split into chunks with overlap
            var chunks = new List<DocumentChunk>();
            int chunkIndex = 0;
            int currentTokenPosition = 0;

            while (currentTokenPosition < allTokenIds.Count)
            {
                // Determine the end position for this chunk
                int endTokenPosition = Math.Min(currentTokenPosition + maxTokens, allTokenIds.Count);
                
                // Extract token IDs for this chunk
                var chunkTokenIds = allTokenIds.Skip(currentTokenPosition).Take(endTokenPosition - currentTokenPosition).ToList();
                
                // Decode tokens back to text
                string chunkText = tokenizer.Decode(chunkTokenIds);
                
                // Calculate character positions in the original text
                // We need to find where this chunk starts and ends in the original text
                int startCharPosition = GetCharacterPosition(tokenizer, allTokenIds, currentTokenPosition);
                int endCharPosition = GetCharacterPosition(tokenizer, allTokenIds, endTokenPosition);
                
                var chunk = new DocumentChunk
                {
                    ChunkId = Guid.NewGuid().ToString(),
                    DocumentId = documentId,
                    Text = chunkText,
                    ChunkIndex = chunkIndex,
                    StartPosition = startCharPosition,
                    EndPosition = endCharPosition
                };
                
                chunks.Add(chunk);
                chunkIndex++;
                
                // Move to next chunk position with overlap
                // If this is the last chunk, we're done
                if (endTokenPosition >= allTokenIds.Count)
                {
                    break;
                }
                
                // Move forward by (maxTokens - overlapTokens) to create overlap
                currentTokenPosition += (maxTokens - overlapTokens);
            }

            _logger.LogInformation(
                "Text chunked into {ChunkCount} chunks for document {DocumentId}. Total tokens: {TokenCount}",
                chunks.Count, documentId, allTokenIds.Count);

            return Task.FromResult(chunks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error chunking text for document {DocumentId}", documentId);
            throw new ProcessingException("Failed to chunk text.", ex);
        }
    }

    /// <summary>
    /// Gets the character position in the original text for a given token position.
    /// </summary>
    private int GetCharacterPosition(Tokenizer tokenizer, IReadOnlyList<int> allTokenIds, int tokenPosition)
    {
        if (tokenPosition == 0)
        {
            return 0;
        }
        
        if (tokenPosition >= allTokenIds.Count)
        {
            // Decode all tokens to get the full text length
            var fullText = tokenizer.Decode(allTokenIds);
            return fullText.Length;
        }
        
        // Decode tokens up to this position to get character position
        var tokensUpToPosition = allTokenIds.Take(tokenPosition).ToList();
        var textUpToPosition = tokenizer.Decode(tokensUpToPosition);
        return textUpToPosition.Length;
    }

    /// <summary>
    /// Extracts text from a PDF document while preserving paragraph structure.
    /// </summary>
    private async Task<string> ExtractTextFromPdfAsync(Stream documentStream, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var pdfReader = new PdfReader(documentStream);
                using var pdfDocument = new PdfDocument(pdfReader);
                
                var textBuilder = new StringBuilder();
                int numberOfPages = pdfDocument.GetNumberOfPages();

                for (int i = 1; i <= numberOfPages; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var page = pdfDocument.GetPage(i);
                    var strategy = new LocationTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        textBuilder.AppendLine(pageText);
                        textBuilder.AppendLine(); // Add paragraph separation between pages
                    }
                }

                return textBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                throw new ProcessingException("Failed to extract text from PDF document.", ex);
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Extracts text from a DOCX document while preserving paragraph structure.
    /// </summary>
    private async Task<string> ExtractTextFromDocxAsync(Stream documentStream, CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var wordDocument = WordprocessingDocument.Open(documentStream, false);
                
                if (wordDocument.MainDocumentPart == null)
                {
                    throw new ProcessingException("DOCX document has no main document part.");
                }

                var body = wordDocument.MainDocumentPart.Document.Body;
                if (body == null)
                {
                    throw new ProcessingException("DOCX document has no body.");
                }

                var textBuilder = new StringBuilder();
                
                // Extract text from paragraphs, preserving paragraph structure
                var paragraphs = body.Descendants<Paragraph>();
                foreach (var paragraph in paragraphs)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    string paragraphText = paragraph.InnerText;
                    if (!string.IsNullOrWhiteSpace(paragraphText))
                    {
                        textBuilder.AppendLine(paragraphText);
                    }
                }

                return textBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from DOCX");
                throw new ProcessingException("Failed to extract text from DOCX document.", ex);
            }
        }, cancellationToken);
    }
}
