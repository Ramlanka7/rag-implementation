namespace RagIndexer.Services;

/// <summary>
/// Defines the contract for generating text embeddings via Azure OpenAI.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates float vector embeddings for a batch of texts.
    /// Returns one <c>float[]</c> per input text, in the same order.
    /// </summary>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);
}
