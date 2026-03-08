using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using RagApi.Options;

namespace RagApi.Services;

/// <summary>
/// Calls gpt-4o with a system prompt and a user question, and returns the answer string.
/// </summary>
public class ChatService : IChatService
{
    private readonly ChatClient _client;
    private readonly ILogger<ChatService> _logger;

    public ChatService(IOptions<AzureOpenAiOptions> options, ILogger<ChatService> logger)
    {
        var opt = options.Value;
        var openAiClient = new AzureOpenAIClient(new Uri(opt.Endpoint), new AzureKeyCredential(opt.ApiKey));
        _client = openAiClient.GetChatClient(opt.ChatDeployment);
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<string> GetAnswerAsync(
        string systemPrompt,
        string question,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Sending chat completion request...");

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(question)
        };

        var response = await _client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        var answer = response.Value.Content[0].Text;

        _logger.LogDebug("Received answer of length {Length}.", answer.Length);
        return answer;
    }
}
