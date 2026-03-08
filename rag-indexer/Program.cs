using RagIndexer;
using RagIndexer.Services;

var builder = Host.CreateApplicationBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<SqlExtractorService>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<SearchIndexerService>();
builder.Services.AddHostedService<Worker>();

// ── Run ───────────────────────────────────────────────────────────────────────
var host = builder.Build();
await host.RunAsync();
