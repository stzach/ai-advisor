using Microsoft.Extensions.Options;
using AiAdvisor.Infrastructure.AI.Services.Options;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;

namespace AiAdvisor.Infrastructure.AI.Services;

public class EmbeddingsProvider : IEmbeddingsProvider
{
    private readonly OpenAIClient _client;
    private readonly string _deployment;

    public EmbeddingsProvider(IOptions<AzureOpenAiOptions> options)
    {
        var openAiOptions = options.Value;
        var endpoint = new Uri(openAiOptions.Endpoint);

        // Use DefaultAzureCredential (managed identity in Azure, developer credentials locally)
        // when no real API key is configured
        _client = string.IsNullOrWhiteSpace(openAiOptions.ApiKey) || openAiOptions.ApiKey == "managed-identity"
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new AzureKeyCredential(openAiOptions.ApiKey));

        _deployment = openAiOptions.EmbeddingDeployment;
    }

    public async Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text)
    {
        EmbeddingClient embeddingClient = _client.GetEmbeddingClient(_deployment);

        ClientResult<OpenAIEmbedding> embeddingResult = embeddingClient.GenerateEmbedding(text);
        if (embeddingResult?.Value != null)
        {
            float[] embedding = embeddingResult.Value.ToFloats().ToArray();

            Console.WriteLine($"Embedding Length: {embedding.Length}");
            Console.WriteLine("Embedding Values:");
            foreach (float value in embedding)
            {
                Console.Write($"{value}, ");
            }
            return embedding;
        }
        else
        {
            Console.WriteLine("Failed to generate embedding or received null value.");
        }

        return Array.Empty<float>();
    }
}