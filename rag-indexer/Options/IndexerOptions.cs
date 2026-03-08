using System.ComponentModel.DataAnnotations;

namespace RagIndexer.Options;

public sealed class IndexerOptions
{
    public const string SectionName = "Indexer";

    [Range(1, 1000, ErrorMessage = "Indexer:BatchSize must be between 1 and 1000.")]
    public int BatchSize { get; set; } = 50;

    [Range(1, 100_000, ErrorMessage = "Indexer:MaxRowsPerTable must be between 1 and 100000.")]
    public int MaxRowsPerTable { get; set; } = 2000;
}
