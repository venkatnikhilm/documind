# Configuration Guide

## User Secrets Setup

DocuMind uses .NET User Secrets to manage sensitive configuration values. This ensures that credentials are never committed to source control.

### Quick Setup

1. Navigate to the DocuMind project directory:
   ```bash
   cd DocuMind
   ```

2. Set all required configuration values using the `dotnet user-secrets` command:

   ```bash
   # Azure OpenAI Configuration
   dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource-name.openai.azure.com/"
   dotnet user-secrets set "AzureOpenAI:Key" "your-azure-openai-api-key"
   dotnet user-secrets set "AzureOpenAI:GptDeployment" "gpt-4o"
   dotnet user-secrets set "AzureOpenAI:EmbeddingDeployment" "text-embedding-3-small"

   # Azure AI Search Configuration
   dotnet user-secrets set "AzureSearch:Endpoint" "https://your-search-service.search.windows.net"
   dotnet user-secrets set "AzureSearch:Key" "your-azure-search-admin-key"
   dotnet user-secrets set "AzureSearch:IndexName" "documind-chunks"

   # Azure Blob Storage Configuration
   dotnet user-secrets set "AzureBlob:ConnectionString" "DefaultEndpointsProtocol=https;AccountName=your-account;AccountKey=your-key;EndpointSuffix=core.windows.net"
   dotnet user-secrets set "AzureBlob:Container" "documind-documents"
   ```

3. Verify your secrets are set:
   ```bash
   dotnet user-secrets list
   ```

### Configuration Validation

The application validates all required configuration values on startup. If any value is missing or empty, the application will fail to start with a descriptive error message indicating which configuration value is missing.

### Required Configuration Values

#### AzureOpenAI Section
- **Endpoint**: The Azure OpenAI service endpoint URL
- **Key**: The API key for authenticating with Azure OpenAI
- **GptDeployment**: The deployment name for the GPT model (e.g., gpt-4o)
- **EmbeddingDeployment**: The deployment name for the embedding model (e.g., text-embedding-3-small)

#### AzureSearch Section
- **Endpoint**: The Azure AI Search service endpoint URL
- **Key**: The API key for authenticating with Azure AI Search
- **IndexName**: The name of the search index for document chunks

#### AzureBlob Section
- **ConnectionString**: The connection string for Azure Blob Storage
- **Container**: The name of the blob container for storing documents

### Template File

A template file `secrets.json.template` is provided in the project root showing the expected structure. This file is for reference only and should not contain actual secrets.

### Production Configuration

For production environments, use Azure Key Vault or environment variables instead of User Secrets. The configuration system will automatically read from environment variables if they are set.

### Troubleshooting

If the application fails to start with a configuration error:

1. Check that all required secrets are set using `dotnet user-secrets list`
2. Verify that the UserSecretsId in DocuMind.csproj matches your secrets location
3. Ensure there are no typos in the configuration keys
4. Check that values are not empty or whitespace-only

For more information on User Secrets, see: https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
