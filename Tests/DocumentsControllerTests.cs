using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DocuMind.Controllers;
using DocuMind.Services;
using DocuMind.Models;
using DocuMind.Plugins;
using Microsoft.SemanticKernel;
using System.Text;

namespace DocuMind.Tests;

public class DocumentsControllerTests
{
    private readonly Mock<IBlobStorageService> _mockBlobService;
    private readonly Mock<IDocumentProcessingService> _mockDocProcessingService;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<ISearchService> _mockSearchService;
    private readonly Mock<ILogger<DocumentsController>> _mockLogger;
    private readonly Kernel _kernel;
    private readonly SearchPlugin _searchPlugin;
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _mockBlobService = new Mock<IBlobStorageService>();
        _mockDocProcessingService = new Mock<IDocumentProcessingService>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockSearchService = new Mock<ISearchService>();
        _mockLogger = new Mock<ILogger<DocumentsController>>();
        
        // Create a real Kernel instance for testing (it's sealed, can't be mocked)
        var kernelBuilder = Kernel.CreateBuilder();
        _kernel = kernelBuilder.Build();
        
        // Create real SearchPlugin with mocked services
        _searchPlugin = new SearchPlugin(_mockSearchService.Object, _mockEmbeddingService.Object);

        _controller = new DocumentsController(
            _mockBlobService.Object,
            _mockDocProcessingService.Object,
            _mockEmbeddingService.Object,
            _mockSearchService.Object,
            _mockLogger.Object,
            _kernel,
            _searchPlugin);
    }

    [Fact]
    public async Task QueryDocument_WithValidRequest_SetsSSEHeaders()
    {
        // Arrange
        var request = new QueryRequest
        {
            Question = "What is the main topic of the document?",
            DocumentId = null
        };

        // Create a mock HTTP context with a response that can be written to
        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        await _controller.QueryDocument(request);

        // Assert
        Assert.Equal("text/event-stream", httpContext.Response.ContentType);
        Assert.True(httpContext.Response.Headers.ContainsKey("Cache-Control"));
        Assert.Equal("no-cache", httpContext.Response.Headers["Cache-Control"].ToString());
        Assert.True(httpContext.Response.Headers.ContainsKey("Connection"));
        Assert.Equal("keep-alive", httpContext.Response.Headers["Connection"].ToString());
    }

    [Fact]
    public async Task QueryDocument_WithValidRequest_SendsTestEvent()
    {
        // Arrange
        var request = new QueryRequest
        {
            Question = "What is the main topic?",
            DocumentId = null
        };

        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        await _controller.QueryDocument(request);

        // Assert
        responseStream.Seek(0, SeekOrigin.Begin);
        var responseText = Encoding.UTF8.GetString(responseStream.ToArray());
        
        Assert.Contains("event:", responseText);
        Assert.Contains("data: {", responseText);
        // SSE connection not asserted - kernel has no AI service in test environment
        // Completion event not asserted - kernel has no AI service in test environment
    }

    [Fact]
    public async Task QueryDocument_WithEmptyQuestion_ReturnsValidationError()
    {
        // Arrange
        var request = new QueryRequest
        {
            Question = "",
            DocumentId = null
        };

        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        await _controller.QueryDocument(request);

        // Assert
        Assert.Equal(400, httpContext.Response.StatusCode);
        Assert.StartsWith("application/json", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task QueryDocument_WithNullRequest_ReturnsValidationError()
    {
        // Arrange
        QueryRequest? request = null;

        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        await _controller.QueryDocument(request!);

        // Assert
        Assert.Equal(400, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task QueryDocument_WithDocumentIdFilter_LogsDocumentId()
    {
        // Arrange
        var request = new QueryRequest
        {
            Question = "What is the summary?",
            DocumentId = "test-doc-123"
        };

        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        await _controller.QueryDocument(request);

        // Assert - verify logging was called with the documentId
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("test-doc-123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
