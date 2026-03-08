using System.Text.Json.Serialization;

namespace RagIndexer.Models;

/// <summary>
/// Represents a single document stored in Azure AI Search.
/// </summary>
public class IndexDocument
{
    /// <summary>Unique key in the Azure AI Search index.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    /// <summary>Human-readable text used for the embedding and LLM context.</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = default!;

    /// <summary>Source table name e.g. "Product", "Customer", "SalesOrderHeader".</summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = default!;

    /// <summary>Original primary key from the source SQL table.</summary>
    [JsonPropertyName("sourceId")]
    public string SourceId { get; set; } = default!;

    /// <summary>1536-dimension float vector produced by text-embedding-3-small.</summary>
    [JsonPropertyName("vector")]
    public IReadOnlyList<float> Vector { get; set; } = default!;
}
