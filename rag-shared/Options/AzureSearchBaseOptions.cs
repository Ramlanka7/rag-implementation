using System.ComponentModel.DataAnnotations;

namespace RagShared.Options;

/// <summary>
/// Common Azure AI Search settings shared by rag-api and rag-indexer.
/// Each project subclasses this to add any project-specific properties.
/// </summary>
public class AzureSearchBaseOptions
{
    public const string SectionName = "AzureSearch";

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureSearch:Endpoint is required.")]
    public string Endpoint { get; set; } = default!;

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureSearch:ApiKey is required.")]
    public string ApiKey { get; set; } = default!;

    public string IndexName { get; set; } = "adventureworks-index";
}
