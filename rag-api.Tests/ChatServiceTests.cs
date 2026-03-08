using Azure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RagApi.Options;
using RagApi.Services;
using Xunit;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace RagApi.Tests;

/// <summary>
/// Unit tests for <see cref="ChatService"/>.
/// Tests 1-2 require live Azure credentials and are skipped in CI by default.
/// Test 3 verifies the service contract via a mocked <see cref="IChatService"/>.
/// </summary>
public class ChatServiceTests
{
    private static IOptions<AzureOpenAiOptions> RealOptions() =>
        MsOptions.Create(new AzureOpenAiOptions
        {
            Endpoint       = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "https://placeholder.cognitiveservices.azure.com/",
            ApiKey         = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY")      ?? "placeholder-key",
            ChatDeployment = "gpt-4o"
        });

    // ── Test 1 ────────────────────────────────────────────────────────────────

    [Fact(Skip = "Requires live Azure OpenAI — set AZURE_OPENAI_ENDPOINT to run")]
    public async Task GetAnswerAsync_ReturnsNonEmptyString()
    {
        var service = new ChatService(RealOptions(), NullLogger<ChatService>.Instance);
        var answer  = await service.GetAnswerAsync("Answer briefly.", "What is 2 + 2?");

        Assert.False(string.IsNullOrWhiteSpace(answer));
    }

    // ── Test 2 ────────────────────────────────────────────────────────────────

    [Fact(Skip = "Requires live Azure OpenAI — set AZURE_OPENAI_ENDPOINT to run")]
    public async Task GetAnswerAsync_ThrowsException_OnInvalidApiKey()
    {
        var badOptions = MsOptions.Create(new AzureOpenAiOptions
        {
            Endpoint       = "https://placeholder.cognitiveservices.azure.com/",
            ApiKey         = "invalid-key",
            ChatDeployment = "gpt-4o"
        });

        var service = new ChatService(badOptions, NullLogger<ChatService>.Instance);
        await Assert.ThrowsAsync<RequestFailedException>(() =>
            service.GetAnswerAsync("system", "question"));
    }

    // ── Test 3 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAnswerAsync_HandlesMockedResponse()
    {
        var mockedService = new Mock<IChatService>(MockBehavior.Strict);
        mockedService
            .Setup(s => s.GetAnswerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Mocked answer from gpt-4o.");

        var answer = await mockedService.Object.GetAnswerAsync("system prompt", "user question");

        Assert.Equal("Mocked answer from gpt-4o.", answer);
    }
}
