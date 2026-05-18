using AiAdvisor.Infrastructure.AI.Models;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace AiAdvisor.Infrastructure.AI.Services;

public class AzureSearchService : IAzureSearchService
{
    private readonly SearchClient _searchClient;

    public AzureSearchService(SearchClient searchClient)
    {
        _searchClient = searchClient;
    }

    public async Task IndexDocumentsAsync(
        IEnumerable<SearchDocumentChunk> chunks,
        CancellationToken cancellationToken)
    {
        var documents = chunks.Select(chunk => new
        {
            id = chunk.Id,
            content = chunk.Content,
            documentId = chunk.Id, // Assuming each chunk is a separate document for simplicity
            fileName = chunk.SourceFileName,
            embedding = chunk.ContentVector
        });

        var batch = IndexDocumentsBatch.Create(
            documents.Select(IndexDocumentsAction.MergeOrUpload).ToArray());

        await _searchClient.IndexDocumentsAsync(
            batch,
            cancellationToken: cancellationToken);
    }
}