using DocuMind.Models;
using DocuMind.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocuMind.Tests;

public class ChunkTextAsyncTests
{
    private readonly DocumentProcessingService _service;
    private readonly Mock<ILogger<DocumentProcessingService>> _mockLogger;

    public ChunkTextAsyncTests()
    {
        _mockLogger = new Mock<ILogger<DocumentProcessingService>>();
        _service = new DocumentProcessingService(_mockLogger.Object);
    }

    [Fact]
    public async Task ChunkTextAsync_EmptyText_ReturnsEmptyList()
    {
        // Arrange
        string text = "";
        string documentId = "doc123";

        // Act
        var result = await _service.ChunkTextAsync(text, documentId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ChunkTextAsync_ShortText_ReturnsSingleChunk()
    {
        // Arrange
        string text = "This is a short text that should fit in a single chunk.";
        string documentId = "doc123";

        // Act
        var result = await _service.ChunkTextAsync(text, documentId);

        // Assert
        Assert.Single(result);
        Assert.Equal(text, result[0].Text);
        Assert.Equal(documentId, result[0].DocumentId);
        Assert.Equal(0, result[0].ChunkIndex);
        Assert.Equal(0, result[0].StartPosition);
        Assert.Equal(text.Length, result[0].EndPosition);
        Assert.NotEmpty(result[0].ChunkId);
    }

    [Fact]
    public async Task ChunkTextAsync_LongText_ReturnsMultipleChunks()
    {
        // Arrange
        // Create a text that will require multiple chunks (500 tokens each)
        string sentence = "This is a sentence that will be repeated many times to create a long text. ";
        string text = string.Concat(Enumerable.Repeat(sentence, 100)); // ~1500 tokens
        string documentId = "doc456";

        // Act
        var result = await _service.ChunkTextAsync(text, documentId, maxTokens: 500, overlapTokens: 50);

        // Assert
        Assert.True(result.Count > 1, "Should have multiple chunks");
        
        // Verify each chunk has unique ChunkId
        var chunkIds = result.Select(c => c.ChunkId).ToList();
        Assert.Equal(chunkIds.Count, chunkIds.Distinct().Count());
        
        // Verify chunk indices are sequential
        for (int i = 0; i < result.Count; i++)
        {
            Assert.Equal(i, result[i].ChunkIndex);
            Assert.Equal(documentId, result[i].DocumentId);
        }
        
        // Verify positions are sequential
        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].StartPosition < result[i].EndPosition, 
                $"Chunk {i} start should be before end");
            Assert.True(result[i].EndPosition <= result[i + 1].EndPosition, 
                $"Chunk {i} end should be before or equal to next chunk end");
        }
    }

    [Fact]
    public async Task ChunkTextAsync_AllChunks_HaveValidMetadata()
    {
        // Arrange
        string sentence = "The quick brown fox jumps over the lazy dog. ";
        string text = string.Concat(Enumerable.Repeat(sentence, 80));
        string documentId = "doc789";

        // Act
        var result = await _service.ChunkTextAsync(text, documentId);

        // Assert
        foreach (var chunk in result)
        {
            Assert.NotEmpty(chunk.ChunkId);
            Assert.Equal(documentId, chunk.DocumentId);
            Assert.NotEmpty(chunk.Text);
            Assert.True(chunk.StartPosition >= 0);
            Assert.True(chunk.EndPosition > chunk.StartPosition);
            Assert.True(chunk.ChunkIndex >= 0);
        }
    }
}
