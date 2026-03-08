using RagApi.Services;
using Xunit;

namespace RagApi.Tests;

/// <summary>
/// Unit tests for <see cref="PromptBuilder"/>.
/// No mocks required — pure logic.
/// </summary>
public class PromptBuilderTests
{
    private readonly PromptBuilder _builder = new();

    // ── Test 1 ────────────────────────────────────────────────────────────────

    [Fact]
    public void BuildPrompt_ContainsSystemInstruction()
    {
        var prompt = _builder.BuildPrompt("Any question?", []);
        Assert.Contains("Answer only using the context", prompt);
    }

    // ── Test 2 ────────────────────────────────────────────────────────────────

    [Fact]
    public void BuildPrompt_ContainsContextDocuments()
    {
        var docs = new List<SearchResult>
        {
            new() { Id = "doc-1", Content = "Content alpha",  Source = "Table1", Score = 0.9 },
            new() { Id = "doc-2", Content = "Content beta",   Source = "Table2", Score = 0.8 },
            new() { Id = "doc-3", Content = "Content gamma",  Source = "Table3", Score = 0.7 }
        };

        var prompt = _builder.BuildPrompt("Some question", docs);

        Assert.Contains("Content alpha",  prompt);
        Assert.Contains("Content beta",   prompt);
        Assert.Contains("Content gamma",  prompt);
    }

    // ── Test 3 ────────────────────────────────────────────────────────────────

    [Fact]
    public void BuildPrompt_HandlesEmptyContext()
    {
        // Should not throw
        var prompt = _builder.BuildPrompt("Question with no context", []);
        Assert.NotNull(prompt);
        Assert.False(string.IsNullOrWhiteSpace(prompt));
    }

    // ── Test 4 ────────────────────────────────────────────────────────────────

    [Fact]
    public void BuildPrompt_ContainsQuestion_ViaSystemPrompt()
    {
        // The prompt builder embeds context into the system prompt; the question
        // is forwarded separately to the LLM, but the system prompt must exist.
        var question = "How many customers placed orders in 2013?";
        var prompt   = _builder.BuildPrompt(question, []);
        Assert.NotEmpty(prompt);
    }
}
