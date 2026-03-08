using Azure;
using Azure.AI.OpenAI;
using OpenAI.Embeddings;
using RagShared.Options;

namespace RagShared.Services;

/// <summary>
/// Creates an <see cref="EmbeddingClient"/> from Azure OpenAI options.
/// Eliminates the duplicated <c>AzureOpenAIClient → GetEmbeddingClient</c>
/// construction that previously lived in both rag-api and rag-indexer.
/// </summary>
public static class EmbeddingClientFactory
{
    /// <summary>
    /// Builds an <see cref="EmbeddingClient"/> from the supplied base options.
    /// </summary>
    public static EmbeddingClient Create(AzureOpenAiBaseOptions options)
    {
        var client = new AzureOpenAIClient(
            new Uri(options.Endpoint),
            new AzureKeyCredential(options.ApiKey));

        return client.GetEmbeddingClient(options.EmbeddingDeployment);
    }
}
