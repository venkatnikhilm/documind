using DocuMind.Models;
using DocuMind.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174") // Vite dev server ports
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Type"); // For SSE
    });
});

// Configure structured logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Set logging levels from configuration
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

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
builder.Services.AddSingleton<DocuMind.Services.ISearchService, DocuMind.Services.SearchService>();

// Configure Semantic Kernel with Azure OpenAI
var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(
    deploymentName: azureOpenAIConfig.GptDeployment,
    endpoint: azureOpenAIConfig.Endpoint,
    apiKey: azureOpenAIConfig.Key);

var kernel = kernelBuilder.Build();

// Register Semantic Kernel as a singleton
builder.Services.AddSingleton(kernel);

// Register plugins with dependency injection
builder.Services.AddSingleton<SearchPlugin>();
builder.Services.AddSingleton<SummarizePlugin>();

// Configure HTTP logging middleware
builder.Services.AddHttpLogging(logging =>
{
    // Log request and response information
    logging.LoggingFields = HttpLoggingFields.RequestPath
                          | HttpLoggingFields.RequestMethod
                          | HttpLoggingFields.RequestQuery
                          | HttpLoggingFields.ResponseStatusCode
                          | HttpLoggingFields.Duration;
    
    // Set request body logging limit (in bytes)
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
    
    // Configure media types to log
    logging.MediaTypeOptions.AddText("application/json");
    logging.MediaTypeOptions.AddText("multipart/form-data");
});

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseCors("AllowFrontend");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Add HTTP logging middleware
app.UseHttpLogging();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
