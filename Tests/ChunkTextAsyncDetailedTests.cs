using DocuMind.Services;
using Microsoft.Extensions.Logging;
using Microsoft.ML.Tokenizers;
using Moq;
using Xunit;

namespace DocuMind.Tests;

public class ChunkTextAsyncDetailedTests
{
    private readonly DocumentProcessingService _service;
    private readonly Mock<ILogger<DocumentProcessingService>> _mockLogger;
    private readonly Tokenizer _tokenizer;

    public ChunkTextAsyncDetailedTests()
    {
        _mockLogger = new Mock<ILogger<DocumentProcessingService>>();
        _service = new DocumentProcessingService(_mockLogger.Object);
        _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4");
    }

    [Fact]
    public async Task ChunkTextAsync_AllChunks_RespectMaxTokenLimit()
    {
        // Arrange
        string sentence = "The quick brown fox jumps over the lazy dog. ";
        string text = string.Concat(Enumerable.Repeat(sentence, 100));
        string documentId = "doc123";
        int maxTokens = 500;

        // Act
        var result = await _service.ChunkTextAsync(text, documentId, maxTokens, 50);

        // Assert
        foreach (var chunk in result)
        {
            var tokenIds = _tokenizer.EncodeToIds(chunk.Text);
            Assert.True(tokenIds.Count <= maxTokens, 
                $"Chunk {chunk.ChunkIndex} has {tokenIds.Count} tokens, exceeds max of {maxTokens}");
        }
    }

    [Fact]
    public async Task ChunkTextAsync_ConsecutiveChunks_HaveOverlap()
    {
        // Arrange
        string sentence = "The quick brown fox jumps over the lazy dog. ";
        string text = string.Concat(Enumerable.Repeat(sentence, 100));
        string documentId = "doc456";
        int maxTokens = 500;
        int overlapTokens = 50;

        // Act
        var result = await _service.ChunkTextAsync(text, documentId, maxTokens, overlapTokens);

        // Assert - verify we have multiple chunks
        Assert.True(result.Count > 1, "Should have multiple chunks for this test");

        // Verify overlap exists between consecutive chunks
        for (int i = 0; i < result.Count - 1; i++)
        {
            var currentChunk = result[i];
            var nextChunk = result[i + 1];

            // Check that there's some text overlap based on character positions
            // The next chunk should start before the current chunk ends (in terms of original text position)
            Assert.True(nextChunk.StartPosition < currentChunk.EndPosition,
                $"Chunks {i} and {i + 1} should have overlapping positions");
        }
    }

    [Fact]
    public async Task ChunkTextAsync_CharacterPositions_AreSequential()
    {
        // Arrange
        string sentence = "The quick brown fox jumps over the lazy dog. ";
        string text = string.Concat(Enumerable.Repeat(sentence, 80));
        string documentId = "doc789";

        // Act
        var result = await _service.ChunkTextAsync(text, documentId);

        // Assert
        Assert.True(result.Count > 0, "Should have at least one chunk");

        // First chunk should start at position 0
        Assert.Equal(0, result[0].StartPosition);

        // Last chunk should end at or near the text length
        var lastChunk = result[result.Count - 1];
        Assert.True(lastChunk.EndPosition <= text.Length,
            $"Last chunk end position {lastChunk.EndPosition} should not exceed text length {text.Length}");

        // All chunks should have valid positions
        foreach (var chunk in result)
        {
            Assert.True(chunk.StartPosition >= 0, "Start position should be non-negative");
            Assert.True(chunk.EndPosition > chunk.StartPosition, "End position should be after start position");
            Assert.True(chunk.EndPosition <= text.Length, "End position should not exceed text length");
        }
    }

    [Fact]
    public async Task ChunkTextAsync_WithCustomTokenLimits_RespectsLimits()
    {
        // Arrange
        string sentence = "This is a test sentence. ";
        string text = string.Concat(Enumerable.Repeat(sentence, 50));
        string documentId = "doc999";
        int maxTokens = 100;
        int overlapTokens = 20;

        // Act
        var result = await _service.ChunkTextAsync(text, documentId, maxTokens, overlapTokens);

        // Assert
        foreach (var chunk in result)
        {
            var tokenIds = _tokenizer.EncodeToIds(chunk.Text);
            Assert.True(tokenIds.Count <= maxTokens,
                $"Chunk {chunk.ChunkIndex} has {tokenIds.Count} tokens, exceeds max of {maxTokens}");
        }
    }

    [Fact]
    public async Task ChunkTextAsync_NullText_ReturnsEmptyList()
    {
        // Arrange
        string? text = null;
        string documentId = "doc000";

        // Act
        var result = await _service.ChunkTextAsync(text!, documentId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ChunkTextAsync_WhitespaceText_ReturnsEmptyList()
    {
        // Arrange
        string text = "   \n\t  ";
        string documentId = "doc001";

        // Act
        var result = await _service.ChunkTextAsync(text, documentId);

        // Assert
        Assert.Empty(result);
    }
}
