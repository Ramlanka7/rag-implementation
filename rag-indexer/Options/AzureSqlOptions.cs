using System.ComponentModel.DataAnnotations;

namespace RagIndexer.Options;

public sealed class AzureSqlOptions
{
    public const string SectionName = "AzureSQL";

    [Required(AllowEmptyStrings = false, ErrorMessage = "AzureSQL:ConnectionString is required.")]
    public string ConnectionString { get; set; } = default!;
}
