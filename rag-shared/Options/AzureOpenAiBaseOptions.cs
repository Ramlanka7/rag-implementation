using System.ComponentModel.DataAnnotations;

namespace RagShared.Options;

/// <summary>
/// Common Azure OpenAI settings shared by rag-api and rag-indexer.
/// Each project subclasses this to add any project-specific properties.
/// </summary>
public class AzureOpenAiBaseOptions
{
    public const string SectionName = "AzureOpenAI";

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureOpenAI:Endpoint is required.")]
    public string Endpoint { get; set; } = default!;

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureOpenAI:ApiKey is required.")]
    public string ApiKey { get; set; } = default!;

    public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";
}
