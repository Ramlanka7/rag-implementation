using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using RagIndexer;
using RagIndexer.Models;
using RagIndexer.Options;
using RagIndexer.Services;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace RagIndexer.Tests;

/// <summary>
/// Unit tests for <see cref="Worker"/>.
/// All external dependencies are mocked — no Azure services required.
/// </summary>
public class WorkerTests
{
    private readonly Mock<ISqlExtractorService>  _extractor = new(MockBehavior.Strict);
    private readonly Mock<IEmbeddingService>     _embedder  = new(MockBehavior.Strict);
    private readonly Mock<ISearchIndexerService> _searcher  = new(MockBehavior.Strict);

    // ── Factory ──────────────────────────────────────────────────────────────

    private Worker CreateWorker(int batchSize = 10) =>
        new(_extractor.Object, _embedder.Object, _searcher.Object,
            MsOptions.Create(new IndexerOptions { BatchSize = batchSize }),
            NullLogger<Worker>.Instance);

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Turns a regular enumerable into an IAsyncEnumerable.</summary>
    private static async IAsyncEnumerable<IndexDocument> ToAsyncEnum(
        IEnumerable<IndexDocument> docs)
    {
        foreach (var doc in docs)
        {
            yield return doc;
            await Task.Yield();
        }
    }

    private static IndexDocument MakeDoc(string id, string? content = null) => new()
    {
        Id       = id,
        Content  = content ?? $"Content for document {id}",
        Source   = "Test",
        SourceId = id
    };

    /// <summary>
    /// Configures the mock embedder to return a 1536-dimension zero vector per text.
    /// </summary>
    private void SetupEmbedder() =>
        _embedder
            .Setup(e => e.GenerateEmbeddingsAsync(
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns((IReadOnlyList<string> texts, CancellationToken _) =>
                Task.FromResult<IReadOnlyList<float[]>>(
                    texts.Select(_ => new float[1536]).ToList()));

    /// <summary>Sets up the search service to accept any upload and ensure-index call.</summary>
    private void SetupSearcher()
    {
        _searcher
            .Setup(s => s.EnsureIndexExistsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _searcher
            .Setup(s => s.UploadDocumentsAsync(
                It.IsAny<IReadOnlyList<IndexDocument>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    // ── Tests ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_EmptySource_OnlyEnsuresIndex()
    {
        _extractor.Setup(x => x.ExtractAllAsync()).Returns(ToAsyncEnum([]));
        _searcher
            .Setup(s => s.EnsureIndexExistsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var worker = CreateWorker();
        await worker.RunIndexingAsync(CancellationToken.None);

        _searcher.Verify(s => s.EnsureIndexExistsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _embedder.Verify(e => e.GenerateEmbeddingsAsync(
            It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
        _searcher.Verify(s => s.UploadDocumentsAsync(
            It.IsAny<IReadOnlyList<IndexDocument>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_DocsBelowBatchSize_FlushesAsSinglePartialBatch()
    {
        var docs = new[] { MakeDoc("1"), MakeDoc("2"), MakeDoc("3") };
        _extractor.Setup(x => x.ExtractAllAsync()).Returns(ToAsyncEnum(docs));
        SetupEmbedder();
        SetupSearcher();

        using var worker = CreateWorker(batchSize: 10);
        await worker.RunIndexingAsync(CancellationToken.None);

        _searcher.Verify(s => s.UploadDocumentsAsync(
            It.Is<IReadOnlyList<IndexDocument>>(d => d.Count == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_DocsSpanTwoBatches_UploadCalledTwice()
    {
        var docs = Enumerable.Range(1, 4).Select(i => MakeDoc(i.ToString())).ToArray();
        _extractor.Setup(x => x.ExtractAllAsync()).Returns(ToAsyncEnum(docs));
        SetupEmbedder();
        SetupSearcher();

        using var worker = CreateWorker(batchSize: 2);
        await worker.RunIndexingAsync(CancellationToken.None);

        // Two full batches of 2
        _searcher.Verify(s => s.UploadDocumentsAsync(
            It.Is<IReadOnlyList<IndexDocument>>(d => d.Count == 2),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_FiveDocsWithBatchOfThree_TwoBatchesUploaded()
    {
        // Batch 1: docs 1-3, Batch 2 (flush): docs 4-5
        var docs = Enumerable.Range(1, 5).Select(i => MakeDoc(i.ToString())).ToArray();
        _extractor.Setup(x => x.ExtractAllAsync()).Returns(ToAsyncEnum(docs));
        SetupEmbedder();
        SetupSearcher();

        using var worker = CreateWorker(batchSize: 3);
        await worker.RunIndexingAsync(CancellationToken.None);

        _searcher.Verify(s => s.UploadDocumentsAsync(
            It.IsAny<IReadOnlyList<IndexDocument>>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_NullContent_DocumentSkipped()
    {
        // Explicitly construct with null content — MakeDoc's ?? operator would coalesce null to a default string
        var docs = new IndexDocument[]
        {
            new() { Id = "1", Content = null!, Source = "Test", SourceId = "1" },
            MakeDoc("2")
        };
        _extractor.Setup(x => x.ExtractAllAsync()).Returns(ToAsyncEnum(docs));
        SetupEmbedder();
        SetupSearcher();

        using var worker = CreateWorker(batchSize: 10);
        await worker.RunIndexingAsync(CancellationToken.None);

        // Only doc "2" should reach the upload; doc "1" must be filtered out
        _searcher.Verify(s => s.UploadDocumentsAsync(
            It.Is<IReadOnlyList<IndexDocument>>(d => d.Count == 1 && d[0].Id == "2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhitespaceOnlyContent_DocumentSkipped()
    {
        var docs = new[] { MakeDoc("1", "   "), MakeDoc("2") };
        _extractor.Setup(x => x.ExtractAllAsync()).Returns(ToAsyncEnum(docs));
        SetupEmbedder();
        SetupSearcher();

        using var worker = CreateWorker(batchSize: 10);
        await worker.RunIndexingAsync(CancellationToken.None);

        _searcher.Verify(s => s.UploadDocumentsAsync(
            It.Is<IReadOnlyList<IndexDocument>>(d => d.Count == 1 && d[0].Id == "2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OversizedContent_ContentTruncatedTo32000Chars()
    {
        const int overLimit = 40_000;
        const int maxLimit  = 32_000;

        var docs = new[] { MakeDoc("1", new string('x', overLimit)) };
        _extractor.Setup(x => x.ExtractAllAsync()).Returns(ToAsyncEnum(docs));
        SetupEmbedder();

        IReadOnlyList<IndexDocument>? captured = null;
        _searcher
            .Setup(s => s.EnsureIndexExistsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _searcher
            .Setup(s => s.UploadDocumentsAsync(
                It.IsAny<IReadOnlyList<IndexDocument>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<IndexDocument>, CancellationToken>((d, _) => captured = d)
            .Returns(Task.CompletedTask);

        using var worker = CreateWorker(batchSize: 10);
        await worker.RunIndexingAsync(CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(maxLimit, captured![0].Content.Length);
    }

    [Fact]
    public async Task ExecuteAsync_ContentAtExactLimit_NotTruncated()
    {
        var docs = new[] { MakeDoc("1", new string('x', 32_000)) };
        _extractor.Setup(x => x.ExtractAllAsync()).Returns(ToAsyncEnum(docs));
        SetupEmbedder();

        IReadOnlyList<IndexDocument>? captured = null;
        _searcher
            .Setup(s => s.EnsureIndexExistsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _searcher
            .Setup(s => s.UploadDocumentsAsync(
                It.IsAny<IReadOnlyList<IndexDocument>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<IndexDocument>, CancellationToken>((d, _) => captured = d)
            .Returns(Task.CompletedTask);

        using var worker = CreateWorker(batchSize: 10);
        await worker.RunIndexingAsync(CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(32_000, captured![0].Content.Length);
    }

    [Fact]
    public async Task ExecuteAsync_FirstBatchFails_LogsErrorAndProcessesSecondBatch()
    {
        // 4 docs, batch size 2 → 2 batches; first upload throws
        var docs = Enumerable.Range(1, 4).Select(i => MakeDoc(i.ToString())).ToArray();
        _extractor.Setup(x => x.ExtractAllAsync()).Returns(ToAsyncEnum(docs));
        SetupEmbedder();
        _searcher
            .Setup(s => s.EnsureIndexExistsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        int callCount = 0;
        _searcher
            .Setup(s => s.UploadDocumentsAsync(
                It.IsAny<IReadOnlyList<IndexDocument>>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new HttpRequestException("Simulated transient network error");
                return Task.CompletedTask;
            });

        using var worker = CreateWorker(batchSize: 2);

        // Must not throw — batch errors are swallowed and logged
        await worker.RunIndexingAsync(CancellationToken.None);

        // Both batches were attempted; first failed, second succeeded
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteAsync_VectorsAttachedToDocumentsBeforeUpload()
    {
        var docs = new[] { MakeDoc("1"), MakeDoc("2") };
        _extractor.Setup(x => x.ExtractAllAsync()).Returns(ToAsyncEnum(docs));
        SetupEmbedder();

        IReadOnlyList<IndexDocument>? captured = null;
        _searcher
            .Setup(s => s.EnsureIndexExistsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _searcher
            .Setup(s => s.UploadDocumentsAsync(
                It.IsAny<IReadOnlyList<IndexDocument>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<IndexDocument>, CancellationToken>((d, _) => captured = d)
            .Returns(Task.CompletedTask);

        using var worker = CreateWorker(batchSize: 10);
        await worker.RunIndexingAsync(CancellationToken.None);

        Assert.NotNull(captured);
        Assert.All(captured!, doc =>
        {
            Assert.NotNull(doc.Vector);
            Assert.Equal(1536, doc.Vector.Count);
        });
    }
}
