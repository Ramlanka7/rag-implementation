using Microsoft.AspNetCore.Mvc;
using RagApi.Models;
using RagApi.Services;

namespace RagApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly PromptBuilder _promptBuilder;
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IEmbeddingService embeddingService,
        IVectorSearchService vectorSearchService,
        PromptBuilder promptBuilder,
        IChatService chatService,
        ILogger<ChatController> logger)
    {
        _embeddingService   = embeddingService;
        _vectorSearchService = vectorSearchService;
        _promptBuilder      = promptBuilder;
        _chatService        = chatService;
        _logger             = logger;
    }

    /// <summary>
    /// Runs the full RAG pipeline and returns an AI-generated answer.
    /// </summary>
    /// <param name="request">The chat request containing the question.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Post(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        _logger.LogInformation("Processing question: {Question}", request.Question);

        // 1. Generate embedding for the question
        var vector = await _embeddingService.GenerateEmbeddingAsync(request.Question, cancellationToken);

        // 2. Retrieve top-K context documents from Azure AI Search
        var searchResults = await _vectorSearchService.SearchAsync(vector, cancellationToken: cancellationToken);

        // 3. Build the system prompt with context
        var systemPrompt = _promptBuilder.BuildPrompt(request.Question, searchResults);

        // 4. Get the answer from gpt-4o
        var answer = await _chatService.GetAnswerAsync(systemPrompt, request.Question, cancellationToken);

        // 5. Return response
        var sources = searchResults.Select(r => r.Id).ToList();
        return Ok(new ChatResponse { Answer = answer, Sources = sources });
    }
}
