using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using RagApi.Options;
using RagShared.Services;

namespace RagApi.Services;

/// <summary>
/// Converts a single question string into a float[] vector using text-embedding-3-small.
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly ILogger<EmbeddingService> _logger;

    public EmbeddingService(IOptions<AzureOpenAiOptions> options, ILogger<EmbeddingService> logger)
    {
        _client = EmbeddingClientFactory.Create(options.Value);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embedding for question...");
        var result = await _client.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        return result.Value.ToFloats().ToArray();
    }
}
