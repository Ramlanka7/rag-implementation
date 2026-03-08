using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RagIndexer.Models;
using RagIndexer.Services;

namespace RagIndexer;

/// <summary>
/// Orchestrates the full indexing pipeline:
///   SQL extract → batch embed → upload to Azure AI Search
/// </summary>
public class Worker : BackgroundService
{
    private readonly SqlExtractorService   _extractor;
    private readonly EmbeddingService      _embedder;
    private readonly SearchIndexerService  _searcher;
    private readonly int                   _batchSize;
    private readonly ILogger<Worker>       _logger;

    public Worker(
        SqlExtractorService  extractor,
        EmbeddingService     embedder,
        SearchIndexerService searcher,
        IConfiguration       config,
        ILogger<Worker>      logger)
    {
        _extractor = extractor;
        _embedder  = embedder;
        _searcher  = searcher;
        _batchSize = config.GetValue<int>("Indexer:BatchSize", 50);
        _logger    = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("=== RAG Indexer started ===");

        // 1. Ensure the Azure AI Search index exists
        await _searcher.EnsureIndexExistsAsync(stoppingToken);

        // 2. Stream all documents from SQL in a rolling buffer
        var buffer = new List<IndexDocument>(_batchSize);
        int totalIndexed = 0;

        await foreach (var doc in _extractor.ExtractAllAsync().WithCancellation(stoppingToken))
        {
            buffer.Add(doc);

            if (buffer.Count >= _batchSize)
            {
                await ProcessBatchAsync(buffer, stoppingToken);
                totalIndexed += buffer.Count;
                _logger.LogInformation("Progress: {Total} documents indexed so far.", totalIndexed);
                buffer.Clear();
            }
        }

        // 3. Flush any remaining documents in the last partial batch
        if (buffer.Count > 0)
        {
            await ProcessBatchAsync(buffer, stoppingToken);
            totalIndexed += buffer.Count;
        }

        _logger.LogInformation("=== Indexing complete. Total documents indexed: {Total} ===", totalIndexed);
    }

    // ─────────────────────────────────────────────────────────────
    //  Batch: embed all texts in one API call, then upload
    // ─────────────────────────────────────────────────────────────

    private async Task ProcessBatchAsync(
        List<IndexDocument> batch,
        CancellationToken   cancellationToken)
    {
        // Generate embeddings for all content texts in one API call
        var texts      = batch.Select(d => d.Content).ToList();
        var vectors    = await _embedder.GenerateEmbeddingsAsync(texts, cancellationToken);

        // Attach the vector to each document
        for (int i = 0; i < batch.Count; i++)
            batch[i].Vector = vectors[i];

        // Upload to Azure AI Search
        await _searcher.UploadDocumentsAsync(batch, cancellationToken);
    }
}
