using RagIndexer.Models;

namespace RagIndexer.Services;

/// <summary>
/// Defines the contract for managing an Azure AI Search index and uploading documents.
/// </summary>
public interface ISearchIndexerService
{
    /// <summary>
    /// Creates the Azure AI Search index if it does not already exist.
    /// Safe to call on every startup.
    /// </summary>
    Task EnsureIndexExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads (merge-or-upload) a batch of documents into Azure AI Search.
    /// </summary>
    Task UploadDocumentsAsync(
        IReadOnlyList<IndexDocument> documents,
        CancellationToken cancellationToken = default);
}
