using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Options;
using RagApi.Options;

namespace RagApi.Services;

/// <summary>
/// Queries Azure AI Search using a vector and returns the top-K matching documents.
/// </summary>
public class VectorSearchService : IVectorSearchService
{
    private readonly SearchClient _searchClient;
    private readonly int _defaultTopK;
    private readonly ILogger<VectorSearchService> _logger;

    public VectorSearchService(IOptions<AzureSearchOptions> options, ILogger<VectorSearchService> logger)
    {
        var opt = options.Value;

        if (string.IsNullOrWhiteSpace(opt.Endpoint))
            throw new InvalidOperationException("AzureSearch:Endpoint is not configured.");

        _searchClient = new SearchClient(new Uri(opt.Endpoint), opt.IndexName, new AzureKeyCredential(opt.ApiKey));
        _defaultTopK = opt.TopK;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<List<SearchResult>> SearchAsync(
        float[] vector,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        int effectiveTopK = topK > 0 ? topK : _defaultTopK;
        _logger.LogDebug("Performing vector search with topK={TopK}...", effectiveTopK);

        var vectorQuery = new VectorizedQuery(vector)
        {
            KNearestNeighborsCount = effectiveTopK,
            Fields = { "vector" }
        };

        var searchOptions = new SearchOptions
        {
            Size = effectiveTopK
        };
        searchOptions.VectorSearch = new VectorSearchOptions();
        searchOptions.VectorSearch.Queries.Add(vectorQuery);
        searchOptions.Select.Add("id");
        searchOptions.Select.Add("content");
        searchOptions.Select.Add("source");

        var response = await _searchClient.SearchAsync<SearchDocument>(searchText: null, options: searchOptions, cancellationToken: cancellationToken);

        var results = new List<SearchResult>();
        await foreach (var result in response.Value.GetResultsAsync())
        {
            results.Add(new SearchResult
            {
                Id      = result.Document.TryGetValue("id",      out var id)      ? id?.ToString()      ?? string.Empty : string.Empty,
                Content = result.Document.TryGetValue("content", out var content) ? content?.ToString() ?? string.Empty : string.Empty,
                Source  = result.Document.TryGetValue("source",  out var source)  ? source?.ToString()  ?? string.Empty : string.Empty,
                Score   = result.Score ?? 0
            });
        }

        _logger.LogDebug("Vector search returned {Count} results.", results.Count);
        return results;
    }
}
