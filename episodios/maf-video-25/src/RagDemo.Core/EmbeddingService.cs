using Azure.AI.OpenAI;
using OpenAI.Embeddings;

namespace RagDemo.Core;

public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

public sealed class AzureEmbeddingService(AzureOpenAIClient azureClient, string deploymentName, int dimensions = 1536)
    : IEmbeddingService
{
    private readonly EmbeddingClient _client = azureClient.GetEmbeddingClient(deploymentName);

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var options = new EmbeddingGenerationOptions { Dimensions = dimensions };
        var result = await _client.GenerateEmbeddingAsync(text, options, cancellationToken);
        return result.Value.ToFloats().ToArray();
    }
}
