using Microsoft.Extensions.Options;
using RagApi.Options;
using RagApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Options & Validation ─────────────────────────────────────────────────────
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

// ─── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IEmbeddingService, EmbeddingService>();
builder.Services.AddSingleton<IVectorSearchService, VectorSearchService>();
builder.Services.AddSingleton<PromptBuilder>();
builder.Services.AddSingleton<IChatService, ChatService>();

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ─── MVC / Controllers ────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ─── Build & Configure ────────────────────────────────────────────────────────
var app = builder.Build();

app.UseCors();
app.MapControllers();

app.Run();

// Expose for integration tests
public partial class Program { }
