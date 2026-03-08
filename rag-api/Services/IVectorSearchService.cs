namespace RagApi.Services;

public interface IVectorSearchService
{
    Task<List<SearchResult>> SearchAsync(float[] vector, int topK = 5, CancellationToken cancellationToken = default);
}
