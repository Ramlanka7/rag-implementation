using RagShared.Options;

namespace RagApi.Options;

/// <summary>
/// Extends the shared base with the chat deployment, which is only needed by rag-api.
/// </summary>
public sealed class AzureOpenAiOptions : AzureOpenAiBaseOptions
{
    public string ChatDeployment { get; set; } = "gpt-4o";
}
