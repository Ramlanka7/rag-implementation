using RagShared.Options;

namespace RagApi.Options;

/// <summary>
/// Extends the shared base with TopK, which is only needed by rag-api.
/// </summary>
public sealed class AzureSearchOptions : AzureSearchBaseOptions
{
    public int TopK { get; set; } = 5;
}
