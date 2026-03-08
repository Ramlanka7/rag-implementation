using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Embeddings;

namespace RagIndexer.Services;

/// <summary>
/// Wraps Azure OpenAI text-embedding-3-small to generate float vectors from text.
/// </summary>
public class EmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(IConfiguration config, ILogger<EmbeddingService> logger)
    {
        var endpoint   = new Uri(config["AzureOpenAI:Endpoint"]
            ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is not configured."));
        var apiKey     = new AzureKeyCredential(config["AzureOpenAI:ApiKey"]
            ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is not configured."));
        var deployment = config["AzureOpenAI:EmbeddingDeployment"] ?? "text-embedding-3-small";

        var openAiClient = new AzureOpenAIClient(endpoint, apiKey);
        _client = openAiClient.GetEmbeddingClient(deployment);
        _logger = logger;
    }

    /// <summary>
    /// Generate embeddings for a batch of texts in a single API call (more efficient).
    /// Returns one float[] per input text, in the same order.
    /// </summary>
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embeddings for {Count} texts...", texts.Count);

        var result = await _client.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);

        return result.Value
            .Select(e => e.ToFloats().ToArray())
            .ToList();
    }
}
