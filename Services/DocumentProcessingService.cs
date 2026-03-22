using DocuMind.Exceptions;
using DocuMind.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;
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
        // This will be implemented in task 3.4
        throw new NotImplementedException("ChunkTextAsync will be implemented in task 3.4");
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
