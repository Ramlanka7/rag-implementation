namespace RagApi.Services;

public interface IChatService
{
    Task<string> GetAnswerAsync(string systemPrompt, string question, CancellationToken cancellationToken = default);
}
