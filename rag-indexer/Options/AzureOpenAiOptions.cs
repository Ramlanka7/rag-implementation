using System.ComponentModel.DataAnnotations;

namespace RagIndexer.Options;

public sealed class AzureOpenAiOptions
{
    public const string SectionName = "AzureOpenAI";

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureOpenAI:Endpoint is required.")]
    public string Endpoint { get; set; } = default!;

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureOpenAI:ApiKey is required.")]
    public string ApiKey { get; set; } = default!;

    public string EmbeddingDeployment { get; set; } = "text-embedding-3-small";
}
