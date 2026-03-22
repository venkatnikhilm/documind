using DocuMind.Models;
using Xunit;

namespace DocuMind.Tests;

/// <summary>
/// Unit tests for configuration model validation.
/// </summary>
public class ConfigurationTests
{
    [Fact]
    public void AzureOpenAIConfig_Validate_ThrowsWhenEndpointMissing()
    {
        // Arrange
        var config = new AzureOpenAIConfig
        {
            Key = "test-key",
            GptDeployment = "gpt-4o",
            EmbeddingDeployment = "text-embedding-3-small"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("AzureOpenAI:Endpoint", exception.Message);
    }

    [Fact]
    public void AzureOpenAIConfig_Validate_ThrowsWhenKeyMissing()
    {
        // Arrange
        var config = new AzureOpenAIConfig
        {
            Endpoint = "https://test.openai.azure.com/",
            GptDeployment = "gpt-4o",
            EmbeddingDeployment = "text-embedding-3-small"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("AzureOpenAI:Key", exception.Message);
    }

    [Fact]
    public void AzureOpenAIConfig_Validate_ThrowsWhenGptDeploymentMissing()
    {
        // Arrange
        var config = new AzureOpenAIConfig
        {
            Endpoint = "https://test.openai.azure.com/",
            Key = "test-key",
            EmbeddingDeployment = "text-embedding-3-small"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("AzureOpenAI:GptDeployment", exception.Message);
    }

    [Fact]
    public void AzureOpenAIConfig_Validate_ThrowsWhenEmbeddingDeploymentMissing()
    {
        // Arrange
        var config = new AzureOpenAIConfig
        {
            Endpoint = "https://test.openai.azure.com/",
            Key = "test-key",
            GptDeployment = "gpt-4o"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("AzureOpenAI:EmbeddingDeployment", exception.Message);
    }

    [Fact]
    public void AzureOpenAIConfig_Validate_SucceedsWhenAllValuesPresent()
    {
        // Arrange
        var config = new AzureOpenAIConfig
        {
            Endpoint = "https://test.openai.azure.com/",
            Key = "test-key",
            GptDeployment = "gpt-4o",
            EmbeddingDeployment = "text-embedding-3-small"
        };

        // Act & Assert - should not throw
        config.Validate();
    }

    [Fact]
    public void AzureSearchConfig_Validate_ThrowsWhenEndpointMissing()
    {
        // Arrange
        var config = new AzureSearchConfig
        {
            Key = "test-key",
            IndexName = "test-index"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("AzureSearch:Endpoint", exception.Message);
    }

    [Fact]
    public void AzureSearchConfig_Validate_ThrowsWhenKeyMissing()
    {
        // Arrange
        var config = new AzureSearchConfig
        {
            Endpoint = "https://test.search.windows.net",
            IndexName = "test-index"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("AzureSearch:Key", exception.Message);
    }

    [Fact]
    public void AzureSearchConfig_Validate_ThrowsWhenIndexNameMissing()
    {
        // Arrange
        var config = new AzureSearchConfig
        {
            Endpoint = "https://test.search.windows.net",
            Key = "test-key"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("AzureSearch:IndexName", exception.Message);
    }

    [Fact]
    public void AzureSearchConfig_Validate_SucceedsWhenAllValuesPresent()
    {
        // Arrange
        var config = new AzureSearchConfig
        {
            Endpoint = "https://test.search.windows.net",
            Key = "test-key",
            IndexName = "test-index"
        };

        // Act & Assert - should not throw
        config.Validate();
    }

    [Fact]
    public void AzureBlobConfig_Validate_ThrowsWhenConnectionStringMissing()
    {
        // Arrange
        var config = new AzureBlobConfig
        {
            Container = "test-container"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("AzureBlob:ConnectionString", exception.Message);
    }

    [Fact]
    public void AzureBlobConfig_Validate_ThrowsWhenContainerMissing()
    {
        // Arrange
        var config = new AzureBlobConfig
        {
            ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("AzureBlob:Container", exception.Message);
    }

    [Fact]
    public void AzureBlobConfig_Validate_SucceedsWhenAllValuesPresent()
    {
        // Arrange
        var config = new AzureBlobConfig
        {
            ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;",
            Container = "test-container"
        };

        // Act & Assert - should not throw
        config.Validate();
    }
}
