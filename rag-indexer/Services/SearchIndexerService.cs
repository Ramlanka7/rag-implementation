using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RagIndexer.Models;

namespace RagIndexer.Services;

/// <summary>
/// Creates and manages the Azure AI Search index, and uploads IndexDocuments.
/// </summary>
public class SearchIndexerService
{
    private const int VectorDimensions = 1536; // text-embedding-3-small default

    private readonly SearchIndexClient  _indexClient;
    private readonly SearchClient       _searchClient;
    private readonly string             _indexName;
    private readonly ILogger<SearchIndexerService> _logger;

    public SearchIndexerService(IConfiguration config, ILogger<SearchIndexerService> logger)
    {
        var endpoint  = new Uri(config["AzureSearch:Endpoint"]
            ?? throw new InvalidOperationException("AzureSearch:Endpoint is not configured."));
        var apiKey    = new AzureKeyCredential(config["AzureSearch:ApiKey"]
            ?? throw new InvalidOperationException("AzureSearch:ApiKey is not configured."));
        _indexName    = config["AzureSearch:IndexName"] ?? "adventureworks-index";

        _indexClient  = new SearchIndexClient(endpoint, apiKey);
        _searchClient = new SearchClient(endpoint, _indexName, apiKey);
        _logger       = logger;
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
        try
        {
            await _indexClient.GetIndexAsync(_indexName, cancellationToken);
            _logger.LogInformation("Index '{IndexName}' already exists.", _indexName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogInformation("Index '{IndexName}' not found. Creating...", _indexName);
            await _indexClient.CreateIndexAsync(BuildIndex(), cancellationToken);
            _logger.LogInformation("Index '{IndexName}' created successfully.", _indexName);
        }
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

        var batch = IndexDocumentsBatch.MergeOrUpload(documents);
        var result = await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);

        var failed = result.Value.Results.Count(r => !r.Succeeded);
        if (failed > 0)
            _logger.LogWarning("{Failed}/{Total} documents failed to index.", failed, documents.Count);
        else
            _logger.LogInformation("Uploaded {Count} documents successfully.", documents.Count);
    }
}
