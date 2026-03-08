using System.ComponentModel.DataAnnotations;
using RagIndexer.Options;
using Xunit;

namespace RagIndexer.Tests;

/// <summary>
/// Verifies that the Options classes enforce their data-annotation constraints,
/// which are the guards evaluated at startup via <c>ValidateOnStart()</c>.
/// </summary>
public class OptionsValidationTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IList<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            instance,
            new ValidationContext(instance),
            results,
            validateAllProperties: true);
        return results;
    }

    private static bool IsValid(object instance) => Validate(instance).Count == 0;

    // ── AzureSqlOptions ──────────────────────────────────────────────────────

    [Fact]
    public void AzureSqlOptions_MissingConnectionString_FailsValidation()
    {
        var opts    = new AzureSqlOptions { ConnectionString = null! };
        var results = Validate(opts);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AzureSqlOptions.ConnectionString)));
    }

    [Fact]
    public void AzureSqlOptions_ValidConnectionString_PassesValidation()
    {
        var opts = new AzureSqlOptions { ConnectionString = "Server=tcp:test;Database=db;" };
        Assert.True(IsValid(opts));
    }

    // ── AzureOpenAiOptions ───────────────────────────────────────────────────

    [Fact]
    public void AzureOpenAiOptions_MissingEndpoint_FailsValidation()
    {
        var opts = new AzureOpenAiOptions { Endpoint = null!, ApiKey = "key" };
        Assert.Contains(Validate(opts), r => r.MemberNames.Contains(nameof(AzureOpenAiOptions.Endpoint)));
    }

    [Fact]
    public void AzureOpenAiOptions_MissingApiKey_FailsValidation()
    {
        var opts = new AzureOpenAiOptions { Endpoint = "https://openai.test/", ApiKey = null! };
        Assert.Contains(Validate(opts), r => r.MemberNames.Contains(nameof(AzureOpenAiOptions.ApiKey)));
    }

    [Fact]
    public void AzureOpenAiOptions_ValidValues_PassesValidation()
    {
        var opts = new AzureOpenAiOptions
        {
            Endpoint          = "https://openai.test/",
            ApiKey            = "key",
            EmbeddingDeployment = "text-embedding-3-small"
        };
        Assert.True(IsValid(opts));
    }

    // ── AzureSearchOptions ───────────────────────────────────────────────────

    [Fact]
    public void AzureSearchOptions_MissingEndpoint_FailsValidation()
    {
        var opts = new AzureSearchOptions { Endpoint = null!, ApiKey = "key" };
        Assert.Contains(Validate(opts), r => r.MemberNames.Contains(nameof(AzureSearchOptions.Endpoint)));
    }

    [Fact]
    public void AzureSearchOptions_MissingApiKey_FailsValidation()
    {
        var opts = new AzureSearchOptions { Endpoint = "https://search.test/", ApiKey = null! };
        Assert.Contains(Validate(opts), r => r.MemberNames.Contains(nameof(AzureSearchOptions.ApiKey)));
    }

    [Fact]
    public void AzureSearchOptions_ValidValues_PassesValidation()
    {
        var opts = new AzureSearchOptions
        {
            Endpoint  = "https://search.test/",
            ApiKey    = "key",
            IndexName = "my-index"
        };
        Assert.True(IsValid(opts));
    }

    // ── IndexerOptions ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void IndexerOptions_InvalidBatchSize_FailsValidation(int batchSize)
    {
        var opts = new IndexerOptions { BatchSize = batchSize };
        Assert.Contains(Validate(opts), r => r.MemberNames.Contains(nameof(IndexerOptions.BatchSize)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100_001)]
    public void IndexerOptions_InvalidMaxRowsPerTable_FailsValidation(int maxRows)
    {
        var opts = new IndexerOptions { MaxRowsPerTable = maxRows };
        Assert.Contains(Validate(opts), r => r.MemberNames.Contains(nameof(IndexerOptions.MaxRowsPerTable)));
    }

    [Fact]
    public void IndexerOptions_ValidValues_PassesValidation()
    {
        var opts = new IndexerOptions { BatchSize = 50, MaxRowsPerTable = 2000 };
        Assert.True(IsValid(opts));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1000, 100_000)]
    public void IndexerOptions_BoundaryValues_PassesValidation(int batchSize, int maxRows)
    {
        var opts = new IndexerOptions { BatchSize = batchSize, MaxRowsPerTable = maxRows };
        Assert.True(IsValid(opts));
    }
}
