using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AiAdvisor.Infrastructure.AI.Services.Options;
using Azure;
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

        _client = new OpenAIClient(
            new AzureKeyCredential(openAiOptions.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(openAiOptions.Endpoint)
            });

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