namespace AiAdvisor.Infrastructure.AI.Services;

/// <summary>
/// Service for generating embeddings for text using Azure OpenAI or similar.
/// </summary>
public interface IEmbeddingsProvider
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">Text to generate embedding for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Vector embedding as a list of floats</returns>
    Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text);

    // /// <summary>
    // /// Generates embeddings for multiple texts in batch.
    // /// </summary>
    // Task<IReadOnlyList<IReadOnlyList<float>>> GenerateEmbeddingsBatchAsync(
    //     IReadOnlyList<string> texts,
    //     CancellationToken cancellationToken = default);
}
