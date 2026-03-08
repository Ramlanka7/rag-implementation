using System.ComponentModel.DataAnnotations;

namespace RagApi.Models;

public sealed class ChatRequest
{
    [Required(ErrorMessage = "Question is required.")]
    [MaxLength(1000, ErrorMessage = "Question must not exceed 1000 characters.")]
    public string Question { get; set; } = default!;
}
