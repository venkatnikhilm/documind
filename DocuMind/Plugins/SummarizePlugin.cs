namespace DocuMind.Plugins;

using System.ComponentModel;
using Microsoft.SemanticKernel;

/// <summary>
/// Semantic Kernel plugin that summarizes document chunks using Azure OpenAI gpt-4o.
/// </summary>
public class SummarizePlugin
{
    private readonly Kernel _kernel;

    /// <summary>
    /// Initializes a new instance of the SummarizePlugin class.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance for invoking AI operations.</param>
    public SummarizePlugin(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
    }

    /// <summary>
    /// Summarizes a collection of document chunks using gpt-4o.
    /// </summary>
    /// <param name="chunks">The document chunks to summarize as a string.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A summary of the provided chunks.</returns>
    [KernelFunction("summarize_chunks")]
    [Description("Summarizes a collection of document chunks")]
    public async Task<string> SummarizeChunksAsync(
        [Description("The chunks to summarize")] string chunks,
        CancellationToken cancellationToken = default)
    {
        // Create a prompt for summarization
        var prompt = $@"Please provide a concise summary of the following document chunks:

{chunks}

Summary:";

        // Invoke gpt-4o via the kernel for summarization
        var result = await _kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);

        return result.ToString();
    }
}
