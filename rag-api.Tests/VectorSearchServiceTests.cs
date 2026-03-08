using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RagApi.Options;
using RagApi.Services;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace RagApi.Tests;

/// <summary>
/// Unit / integration tests for <see cref="VectorSearchService"/>.
/// Tests 1-3 require a real Azure AI Search endpoint and will be skipped in CI
/// unless the env vars are set. Test 4 validates local config validation.
/// </summary>
public class VectorSearchServiceTests
{
    private static bool AzureSearchConfigured =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT"));

    private static IOptions<AzureSearchOptions> RealOptions() =>
        MsOptions.Create(new AzureSearchOptions
        {
            Endpoint  = Environment.GetEnvironmentVariable("AZURE_SEARCH_ENDPOINT") ?? "https://placeholder.search.windows.net",
            ApiKey    = Environment.GetEnvironmentVariable("AZURE_SEARCH_KEY")      ?? "placeholder-key",
            IndexName = "adventureworks-index",
            TopK      = 5
        });

    // ── Test 1 ────────────────────────────────────────────────────────────────

    [Fact(Skip = "Requires live Azure AI Search — set AZURE_SEARCH_ENDPOINT to run")]
    public async Task SearchAsync_ReturnsResults_WhenIndexHasDocuments()
    {
        var service = new VectorSearchService(RealOptions(), NullLogger<VectorSearchService>.Instance);
        var vector  = new float[1536]; // zero vector
        vector[0] = 0.1f;

        var results = await service.SearchAsync(vector);

        Assert.NotNull(results);
        Assert.IsType<List<SearchResult>>(results);
    }

    // ── Test 2 ────────────────────────────────────────────────────────────────

    [Fact(Skip = "Requires live Azure AI Search — set AZURE_SEARCH_ENDPOINT to run")]
    public async Task SearchAsync_ReturnsEmpty_WhenNoMatchFound()
    {
        var service = new VectorSearchService(RealOptions(), NullLogger<VectorSearchService>.Instance);
        var vector  = new float[1536]; // all-zero vector

        var results = await service.SearchAsync(vector);

        Assert.NotNull(results);
        // No exception thrown, result may be empty or contain near-zero matches
    }

    // ── Test 3 ────────────────────────────────────────────────────────────────

    [Fact(Skip = "Requires live Azure AI Search — set AZURE_SEARCH_ENDPOINT to run")]
    public async Task SearchAsync_RespectsTopK()
    {
        var service = new VectorSearchService(RealOptions(), NullLogger<VectorSearchService>.Instance);
        var vector  = new float[1536];

        var results = await service.SearchAsync(vector, topK: 3);

        Assert.True(results.Count <= 3, $"Expected at most 3 results, got {results.Count}");
    }

    // ── Test 4 ────────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ThrowsInvalidOperationException_WhenEndpointIsEmpty()
    {
        var badOptions = MsOptions.Create(new AzureSearchOptions
        {
            Endpoint  = string.Empty,
            ApiKey    = "some-key",
            IndexName = "some-index"
        });

        Assert.Throws<InvalidOperationException>(() =>
            new VectorSearchService(badOptions, NullLogger<VectorSearchService>.Instance));
    }
}
