using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;
using Polly;
using Polly.Retry;
using RagIndexer.Options;
using RagShared.Services;

namespace RagIndexer.Services;

/// <summary>
/// Wraps Azure OpenAI text-embedding-3-small to generate float vectors from text.
/// Implements retry with exponential back-off for transient / rate-limit errors.
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _client;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly ResiliencePipeline _retry;

    public EmbeddingService(IOptions<AzureOpenAiOptions> options, ILogger<EmbeddingService> logger)
    {
        _client = EmbeddingClientFactory.Create(options.Value);
        _logger = logger;
        _retry  = BuildRetryPipeline();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Generating embeddings for {Count} texts...", texts.Count);

        return await _retry.ExecuteAsync(async ct =>
        {
            var result = await _client.GenerateEmbeddingsAsync(texts, cancellationToken: ct);
            return (IReadOnlyList<float[]>)result.Value
                .Select(e => e.ToFloats().ToArray())
                .ToList();
        }, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────
    //  Resilience
    // ─────────────────────────────────────────────────────────────

    private ResiliencePipeline BuildRetryPipeline() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 4,
                Delay            = TimeSpan.FromSeconds(2),
                BackoffType      = DelayBackoffType.Exponential,
                UseJitter        = true,
                ShouldHandle     = new PredicateBuilder()
                    .Handle<RequestFailedException>(ex => ex.Status is 429 or 500 or 503)
                    .Handle<HttpRequestException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Embedding retry {Attempt} after {Delay:g}. Reason: {Reason}",
                        args.AttemptNumber + 1, args.RetryDelay, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
}
