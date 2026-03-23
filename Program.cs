using DocuMind.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure User Secrets and configuration
// User Secrets are automatically loaded in Development environment
// For production, use Azure Key Vault or environment variables

// Bind and validate Azure OpenAI configuration
var azureOpenAIConfig = new AzureOpenAIConfig();
builder.Configuration.GetSection("AzureOpenAI").Bind(azureOpenAIConfig);
try
{
    azureOpenAIConfig.Validate();
}
catch (InvalidOperationException ex)
{
    throw new InvalidOperationException(
        $"Configuration validation failed: {ex.Message} " +
        "Please ensure all required values are set in User Secrets. " +
        "Run 'dotnet user-secrets set \"AzureOpenAI:Endpoint\" \"<value>\"' to configure.",
        ex);
}

// Bind and validate Azure Search configuration
var azureSearchConfig = new AzureSearchConfig();
builder.Configuration.GetSection("AzureSearch").Bind(azureSearchConfig);
try
{
    azureSearchConfig.Validate();
}
catch (InvalidOperationException ex)
{
    throw new InvalidOperationException(
        $"Configuration validation failed: {ex.Message} " +
        "Please ensure all required values are set in User Secrets. " +
        "Run 'dotnet user-secrets set \"AzureSearch:Endpoint\" \"<value>\"' to configure.",
        ex);
}

// Bind and validate Azure Blob configuration
var azureBlobConfig = new AzureBlobConfig();
builder.Configuration.GetSection("AzureBlob").Bind(azureBlobConfig);
try
{
    azureBlobConfig.Validate();
}
catch (InvalidOperationException ex)
{
    throw new InvalidOperationException(
        $"Configuration validation failed: {ex.Message} " +
        "Please ensure all required values are set in User Secrets. " +
        "Run 'dotnet user-secrets set \"AzureBlob:ConnectionString\" \"<value>\"' to configure.",
        ex);
}

// Register configuration models as singletons for dependency injection
builder.Services.AddSingleton(azureOpenAIConfig);
builder.Services.AddSingleton(azureSearchConfig);
builder.Services.AddSingleton(azureBlobConfig);

// Register application services
builder.Services.AddSingleton<DocuMind.Services.IBlobStorageService, DocuMind.Services.BlobStorageService>();
builder.Services.AddSingleton<DocuMind.Services.IDocumentProcessingService, DocuMind.Services.DocumentProcessingService>();
builder.Services.AddSingleton<DocuMind.Services.IEmbeddingService, DocuMind.Services.EmbeddingService>();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
