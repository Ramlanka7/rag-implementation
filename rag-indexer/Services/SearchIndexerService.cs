using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RagIndexer.Models;
using RagIndexer.Options;

namespace RagIndexer.Services;

/// <summary>
/// Creates and manages the Azure AI Search index, and uploads IndexDocuments.
/// Implements retry with exponential back-off for transient errors.
/// </summary>
public class SearchIndexerService : ISearchIndexerService
{
    private const int VectorDimensions = 1536; // text-embedding-3-small default

    private readonly SearchIndexClient  _indexClient;
    private readonly SearchClient       _searchClient;
    private readonly string             _indexName;
    private readonly ILogger<SearchIndexerService> _logger;
    private readonly ResiliencePipeline _retry;

    public SearchIndexerService(IOptions<AzureSearchOptions> options, ILogger<SearchIndexerService> logger)
    {
        var opt       = options.Value;
        var endpoint  = new Uri(opt.Endpoint);
        var apiKey    = new AzureKeyCredential(opt.ApiKey);
        _indexName    = opt.IndexName;

        _indexClient  = new SearchIndexClient(endpoint, apiKey);
        _searchClient = new SearchClient(endpoint, _indexName, apiKey);
        _logger       = logger;
        _retry        = BuildRetryPipeline();
    }

    // ─────────────────────────────────────────────────────────────
    //  Index Management
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates the index if it does not already exist.
    /// Safe to call on every startup.
    /// </summary>
    public async Task EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        await _retry.ExecuteAsync(async ct =>
        {
            try
            {
                await _indexClient.GetIndexAsync(_indexName, ct);
                _logger.LogInformation("Index '{IndexName}' already exists.", _indexName);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                _logger.LogInformation("Index '{IndexName}' not found. Creating...", _indexName);
                await _indexClient.CreateIndexAsync(BuildIndex(), ct);
                _logger.LogInformation("Index '{IndexName}' created successfully.", _indexName);
            }
        }, cancellationToken);
    }

    private SearchIndex BuildIndex()
    {
        // HNSW algorithm config
        var vectorSearch = new VectorSearch();
        vectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration("hnsw-config"));
        vectorSearch.Profiles.Add(new VectorSearchProfile("vector-profile", "hnsw-config"));

        var fields = new List<SearchField>
        {
            new SimpleField("id",       SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
            new SearchableField("content"),
            new SimpleField("source",   SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
            new SimpleField("sourceId", SearchFieldDataType.String) { IsFilterable = true },
            new SearchField("vector",   SearchFieldDataType.Collection(SearchFieldDataType.Single))
            {
                IsSearchable          = true,
                VectorSearchDimensions = VectorDimensions,
                VectorSearchProfileName = "vector-profile"
            }
        };

        return new SearchIndex(_indexName)
        {
            Fields      = fields,
            VectorSearch = vectorSearch
        };
    }

    // ─────────────────────────────────────────────────────────────
    //  Document Upload
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Uploads (merge-or-upload) a batch of documents into Azure AI Search.
    /// </summary>
    public async Task UploadDocumentsAsync(
        IReadOnlyList<IndexDocument> documents,
        CancellationToken cancellationToken = default)
    {
        if (documents.Count == 0) return;

        await _retry.ExecuteAsync(async ct =>
        {
            var batch  = IndexDocumentsBatch.MergeOrUpload(documents);
            var result = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: ct);

            var failed = result.Value.Results.Count(r => !r.Succeeded);
            if (failed > 0)
                _logger.LogWarning("{Failed}/{Total} documents failed to index.", failed, documents.Count);
            else
                _logger.LogInformation("Uploaded {Count} documents successfully.", documents.Count);
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
                        "Search upload retry {Attempt} after {Delay:g}. Reason: {Reason}",
                        args.AttemptNumber + 1, args.RetryDelay, args.Outcome.Exception?.Message);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
}
