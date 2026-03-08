using System.ComponentModel.DataAnnotations;

namespace RagIndexer.Options;

public sealed class AzureSearchOptions
{
    public const string SectionName = "AzureSearch";

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureSearch:Endpoint is required.")]
    public string Endpoint { get; set; } = default!;

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureSearch:ApiKey is required.")]
    public string ApiKey { get; set; } = default!;

    public string IndexName { get; set; } = "adventureworks-index";
}
