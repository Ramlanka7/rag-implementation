using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RagIndexer.Models;
using RagIndexer.Options;
using RagIndexer.Services;

namespace RagIndexer;

/// <summary>
/// Orchestrates the full indexing pipeline:
///   SQL extract → guard rails → batch embed → upload to Azure AI Search
/// </summary>
public class Worker : BackgroundService
{
    /// <summary>
    /// Approximate character ceiling that maps to ~8 000 tokens (model limit 8 191).
    /// Documents exceeding this are truncated before embedding.
    /// </summary>
    private const int MaxContentChars = 32_000;

    private readonly ISqlExtractorService   _extractor;
    private readonly IEmbeddingService      _embedder;
    private readonly ISearchIndexerService  _searcher;
    private readonly int                    _batchSize;
    private readonly ILogger<Worker>        _logger;

    public Worker(
        ISqlExtractorService   extractor,
        IEmbeddingService      embedder,
        ISearchIndexerService  searcher,
        IOptions<IndexerOptions> options,
        ILogger<Worker>        logger)
    {
        _extractor = extractor;
        _embedder  = embedder;
        _searcher  = searcher;
        _batchSize = options.Value.BatchSize;
        _logger    = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => RunIndexingAsync(stoppingToken);

    /// <summary>
    /// Core indexing pipeline — <c>internal</c> so unit tests can invoke it
    /// directly without going through <see cref="BackgroundService"/> lifecycle.
    /// </summary>
    internal async Task RunIndexingAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== RAG Indexer started ===");

        // 1. Ensure the Azure AI Search index exists
        await _searcher.EnsureIndexExistsAsync(cancellationToken);

        // 2. Stream all documents from SQL in a rolling buffer
        var buffer = new List<IndexDocument>(_batchSize);
        int totalIndexed  = 0;
        int totalSkipped  = 0;

        await foreach (var doc in _extractor.ExtractAllAsync().WithCancellation(cancellationToken))
        {
            // ── Guard: skip documents with no content ─────────────────────
            if (string.IsNullOrWhiteSpace(doc.Content))
            {
                _logger.LogWarning("Skipping document '{Id}' ({Source}) — content is null or empty.", doc.Id, doc.Source);
                totalSkipped++;
                continue;
            }

            // ── Guard: truncate oversized content ─────────────────────────
            if (doc.Content.Length > MaxContentChars)
            {
                _logger.LogWarning(
                    "Document '{Id}' content truncated from {Original} to {Max} chars.",
                    doc.Id, doc.Content.Length, MaxContentChars);
                doc.Content = doc.Content[..MaxContentChars];
            }

            buffer.Add(doc);

            if (buffer.Count >= _batchSize)
            {
                var indexed = await ProcessBatchAsync(buffer, cancellationToken);
                totalIndexed += indexed;
                _logger.LogInformation("Progress: {Total} documents indexed so far.", totalIndexed);
                buffer.Clear();
            }
        }

        // 3. Flush any remaining documents in the last partial batch
        if (buffer.Count > 0)
            totalIndexed += await ProcessBatchAsync(buffer, cancellationToken);

        _logger.LogInformation(
            "=== Indexing complete. Indexed: {Total} | Skipped: {Skipped} ===",
            totalIndexed, totalSkipped);
    }

    // ─────────────────────────────────────────────────────────────
    //  Batch: embed all texts in one API call, then upload
    //  Returns the number of documents successfully processed.
    //  Logs and recovers from batch-level failures so a single bad
    //  batch cannot abort the entire indexing run.
    // ─────────────────────────────────────────────────────────────

    private async Task<int> ProcessBatchAsync(
        List<IndexDocument> batch,
        CancellationToken   cancellationToken)
    {
        try
        {
            // Generate embeddings for all content texts in one API call
            var texts   = batch.Select(d => d.Content).ToList();
            var vectors = await _embedder.GenerateEmbeddingsAsync(texts, cancellationToken);

            // Attach the vector to each document
            for (int i = 0; i < batch.Count; i++)
                batch[i].Vector = vectors[i];

            // Upload a snapshot so callers can safely mutate (clear) the buffer afterwards
            await _searcher.UploadDocumentsAsync([..batch], cancellationToken);

            return batch.Count;
        }
        catch (OperationCanceledException)
        {
            throw; // propagate graceful shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Batch of {Count} documents failed to process and will be skipped. First id: '{FirstId}'.",
                batch.Count, batch.FirstOrDefault()?.Id);
            return 0;
        }
    }
}
