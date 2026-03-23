using DocuMind.Exceptions;
using DocuMind.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;

namespace DocuMind.Tests;

public class DocumentProcessingServiceTests
{
    private readonly DocumentProcessingService _service;
    private readonly Mock<ILogger<DocumentProcessingService>> _mockLogger;

    public DocumentProcessingServiceTests()
    {
        _mockLogger = new Mock<ILogger<DocumentProcessingService>>();
        _service = new DocumentProcessingService(_mockLogger.Object);
    }

    [Fact]
    public async Task ExtractTextAsync_ValidPdf_ReturnsExtractedText()
    {
        // Arrange
        var pdfStream = CreateSimplePdf("This is a test PDF document.");
        
        // Act
        var result = await _service.ExtractTextAsync(pdfStream, ".pdf");
        
        // Assert
        Assert.Contains("This is a test PDF document", result);
    }

    [Fact]
    public async Task ExtractTextAsync_ValidDocx_ReturnsExtractedText()
    {
        // Arrange
        var docxStream = CreateSimpleDocx("This is a test DOCX document.");
        
        // Act
        var result = await _service.ExtractTextAsync(docxStream, ".docx");
        
        // Assert
        Assert.Contains("This is a test DOCX document", result);
    }

    [Fact]
    public async Task ExtractTextAsync_PdfWithMultipleParagraphs_PreservesStructure()
    {
        // Arrange
        var pdfStream = CreateSimplePdf("First paragraph.", "Second paragraph.", "Third paragraph.");
        
        // Act
        var result = await _service.ExtractTextAsync(pdfStream, ".pdf");
        
        // Assert
        Assert.Contains("First paragraph", result);
        Assert.Contains("Second paragraph", result);
        Assert.Contains("Third paragraph", result);
    }

    [Fact]
    public async Task ExtractTextAsync_DocxWithMultipleParagraphs_PreservesStructure()
    {
        // Arrange
        var docxStream = CreateSimpleDocx("First paragraph.", "Second paragraph.", "Third paragraph.");
        
        // Act
        var result = await _service.ExtractTextAsync(docxStream, ".docx");
        
        // Assert
        Assert.Contains("First paragraph", result);
        Assert.Contains("Second paragraph", result);
        Assert.Contains("Third paragraph", result);
    }

    [Fact]
    public async Task ExtractTextAsync_UnsupportedFormat_ThrowsProcessingException()
    {
        // Arrange
        var stream = new MemoryStream();
        
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ProcessingException>(
            () => _service.ExtractTextAsync(stream, ".txt"));
        
        Assert.Contains("Unsupported file format", exception.Message);
    }

    [Fact]
    public async Task ExtractTextAsync_EmptyPdfStream_ThrowsProcessingException()
    {
        // Arrange
        var emptyStream = new MemoryStream();
        
        // Act & Assert
        await Assert.ThrowsAsync<ProcessingException>(
            () => _service.ExtractTextAsync(emptyStream, ".pdf"));
    }

    [Fact]
    public async Task ExtractTextAsync_EmptyDocxStream_ThrowsProcessingException()
    {
        // Arrange
        var emptyStream = new MemoryStream();
        
        // Act & Assert
        await Assert.ThrowsAsync<ProcessingException>(
            () => _service.ExtractTextAsync(emptyStream, ".docx"));
    }

    // Helper method to create a simple PDF with text
    private static MemoryStream CreateSimplePdf(params string[] paragraphs)
    {
        var stream = new MemoryStream();
        var writer = new PdfWriter(stream);
        writer.SetCloseStream(false);
        var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
        var document = new iText.Layout.Document(pdf);

        foreach (var paragraph in paragraphs)
        {
            document.Add(new iText.Layout.Element.Paragraph(paragraph));
        }

        document.Close();
        stream.Position = 0;
        return stream;
    }

    // Helper method to create a simple DOCX with text
    private static MemoryStream CreateSimpleDocx(params string[] paragraphs)
    {
        var stream = new MemoryStream();
        using (var wordDocument = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document, true))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = mainPart.Document.AppendChild(new Body());

            foreach (var paragraphText in paragraphs)
            {
                var paragraph = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                var run = paragraph.AppendChild(new Run());
                run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(paragraphText));
            }
        }
        stream.Position = 0;
        return stream;
    }
}
