
using AiAdvisor.Infrastructure.AI.Models;

namespace AiAdvisor.Infrastructure.AI.Services;

public interface IAzureSearchService
{
    Task IndexDocumentsAsync(
        IEnumerable<SearchDocumentChunk> chunks,
        CancellationToken cancellationToken);
}