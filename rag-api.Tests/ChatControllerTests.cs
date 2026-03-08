using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RagApi.Controllers;
using RagApi.Models;
using RagApi.Services;
using Xunit;

namespace RagApi.Tests;

/// <summary>
/// Unit tests for <see cref="ChatController"/>.
/// All Azure dependencies are mocked.
/// </summary>
public class ChatControllerTests
{
    private readonly Mock<IEmbeddingService>    _embedding  = new(MockBehavior.Strict);
    private readonly Mock<IVectorSearchService> _search     = new(MockBehavior.Strict);
    private readonly PromptBuilder              _builder    = new();
    private readonly Mock<IChatService>         _chat       = new(MockBehavior.Strict);

    private ChatController CreateController() =>
        new(_embedding.Object, _search.Object, _builder, _chat.Object,
            NullLogger<ChatController>.Instance);

    private static float[] ZeroVector(int size = 1536) => new float[size];

    private static List<SearchResult> SampleResults() =>
    [
        new() { Id = "product-772", Content = "Mountain Bike details...", Source = "Product", Score = 0.9 },
        new() { Id = "orderdetail-1045", Content = "Order line details...", Source = "SalesOrderDetail", Score = 0.8 }
    ];

    // ── Test 1 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ReturnsAnswer_WhenQuestionIsValid()
    {
        // Arrange
        var question = "Which products sold the most in 2013?";
        var results  = SampleResults();

        _embedding.Setup(e => e.GenerateEmbeddingAsync(question, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(ZeroVector());
        _search.Setup(s => s.SearchAsync(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(results);
        _chat.Setup(c => c.GetAnswerAsync(It.IsAny<string>(), question, It.IsAny<CancellationToken>()))
             .ReturnsAsync("The Mountain Bike was the top-selling product in 2013.");

        var controller = CreateController();
        var request    = new ChatRequest { Question = question };

        // Act
        var actionResult = await controller.Post(request, CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ChatResponse>(ok.Value);
        Assert.False(string.IsNullOrEmpty(response.Answer));
    }

    // ── Test 2 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenQuestionIsEmpty()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("Question", "Question is required.");
        var request = new ChatRequest { Question = string.Empty };

        var actionResult = await controller.Post(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    // ── Test 3 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenQuestionIsNull()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("Question", "Question is required.");
        // ModelState already invalid — simulate null body being bound to empty model
        var request = new ChatRequest { Question = null! };

        var actionResult = await controller.Post(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    // ── Test 4 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ReturnsBadRequest_WhenQuestionExceedsMaxLength()
    {
        var controller = CreateController();
        controller.ModelState.AddModelError("Question", "Question must not exceed 1000 characters.");
        var request = new ChatRequest { Question = new string('x', 1001) };

        var actionResult = await controller.Post(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult.Result);
    }

    // ── Test 5 ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Post_ReturnsSources_WhenDocumentsFound()
    {
        var question = "Which products sold the most in 2013?";
        var results  = SampleResults();

        _embedding.Setup(e => e.GenerateEmbeddingAsync(question, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(ZeroVector());
        _search.Setup(s => s.SearchAsync(It.IsAny<float[]>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(results);
        _chat.Setup(c => c.GetAnswerAsync(It.IsAny<string>(), question, It.IsAny<CancellationToken>()))
             .ReturnsAsync("Some answer.");

        var controller   = CreateController();
        var request      = new ChatRequest { Question = question };

        var actionResult = await controller.Post(request, CancellationToken.None);

        var ok       = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ChatResponse>(ok.Value);
        Assert.NotEmpty(response.Sources);
        Assert.Contains("product-772", response.Sources);
        Assert.Contains("orderdetail-1045", response.Sources);
    }
}
