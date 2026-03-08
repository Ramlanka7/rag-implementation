using RagIndexer.Models;

namespace RagIndexer.Services;

/// <summary>
/// Defines the contract for extracting source documents from a SQL database.
/// </summary>
public interface ISqlExtractorService
{
    /// <summary>
    /// Streams all documents from every configured SQL table.
    /// </summary>
    IAsyncEnumerable<IndexDocument> ExtractAllAsync();
}
