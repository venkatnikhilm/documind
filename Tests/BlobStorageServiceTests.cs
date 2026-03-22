using DocuMind.Models;
using DocuMind.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DocuMind.Tests;

/// <summary>
/// Unit tests for BlobStorageService.
/// Tests basic functionality without requiring actual Azure Blob Storage connection.
/// </summary>
public class BlobStorageServiceTests
{
    [Fact]
    public void BlobStorageService_Constructor_InitializesSuccessfully()
    {
        // Arrange
        var config = new AzureBlobConfig
        {
            ConnectionString = "UseDevelopmentStorage=true",
            Container = "test-container"
        };
        var mockLogger = new Mock<ILogger<BlobStorageService>>();

        // Act & Assert - should not throw
        var service = new BlobStorageService(config, mockLogger.Object);
        Assert.NotNull(service);
    }

    [Fact]
    public void BlobStorageService_Constructor_ThrowsWhenConfigInvalid()
    {
        // Arrange
        var config = new AzureBlobConfig
        {
            ConnectionString = "", // Invalid empty connection string
            Container = "test-container"
        };
        var mockLogger = new Mock<ILogger<BlobStorageService>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BlobStorageService(config, mockLogger.Object));
    }
}
