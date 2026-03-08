namespace RagApi.Models;

public sealed class ChatResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<string> Sources { get; set; } = [];
}
