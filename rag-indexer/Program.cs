using RagIndexer;
using RagIndexer.Options;
using RagIndexer.Services;

var builder = Host.CreateApplicationBuilder(args);

// ── Options validation (fail-fast on startup if config is missing/invalid) ───
builder.Services
    .AddOptions<AzureSqlOptions>()
    .BindConfiguration(AzureSqlOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<AzureOpenAiOptions>()
    .BindConfiguration(AzureOpenAiOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<AzureSearchOptions>()
    .BindConfiguration(AzureSearchOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<IndexerOptions>()
    .BindConfiguration(IndexerOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── Services (registered via interfaces for testability) ─────────────────────
builder.Services.AddSingleton<ISqlExtractorService,  SqlExtractorService>();
builder.Services.AddSingleton<IEmbeddingService,     EmbeddingService>();
builder.Services.AddSingleton<ISearchIndexerService, SearchIndexerService>();
builder.Services.AddHostedService<Worker>();

// ── Run ───────────────────────────────────────────────────────────────────────
var host = builder.Build();
await host.RunAsync();
