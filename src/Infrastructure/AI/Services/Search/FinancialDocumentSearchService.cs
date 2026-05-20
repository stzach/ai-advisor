using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using AiAdvisor.Infrastructure.AI.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;


namespace AiAdvisor.Infrastructure.AI.Services;

/// <summary>
/// Service for searching financial documents using vector and hybrid search.
/// </summary>
public interface IFinancialDocumentSearchService
{
    /// <summary>
    /// Performs vector similarity search across financial documents.
    /// </summary>
    /// <param name="query">Search query (will be embedded)</param>
    /// <param name="topK">Number of results to return (default 5)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of search results ranked by relevance</returns>
    Task<IReadOnlyList<FinancialDocumentSearchResult>> SearchDocumentsAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs hybrid search (vector + keyword) across financial documents.
    /// </summary>
    Task<IReadOnlyList<FinancialDocumentSearchResult>> SearchDocumentsByKeywordAsync(
        string keywords,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result from a financial document search.
/// </summary>
public record FinancialDocumentSearchResult
{
    public string Id { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string SourceFileName { get; init; } = string.Empty;
    public string SectionHeading { get; init; } = string.Empty;
    public double RelevanceScore { get; init; }
    public int ChunkIndex { get; init; }
    public int TotalChunks { get; init; }
}

/// <summary>
/// Implementation of financial document search service.
/// </summary>
public class FinancialDocumentSearchService : IFinancialDocumentSearchService
{
    private readonly SearchClient _searchClient;
    private readonly IEmbeddingsProvider _embeddingsProvider;
    private readonly ILogger<FinancialDocumentSearchService> _logger;
    private const string IndexName = "financial-documents";

    public FinancialDocumentSearchService(
        SearchClient searchClient,
        IEmbeddingsProvider embeddingsProvider,
        ILogger<FinancialDocumentSearchService> logger)
    {
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        _embeddingsProvider = embeddingsProvider ?? throw new ArgumentNullException(nameof(embeddingsProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<FinancialDocumentSearchResult>> SearchDocumentsAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Array.Empty<FinancialDocumentSearchResult>();
            }

            _logger.LogDebug("Searching documents with vector query: {Query}", query);

            // Generate embedding for the query
            var queryEmbedding = await _embeddingsProvider.GenerateEmbeddingAsync(query);

            // Prepare vector search options
            var searchOptions = new SearchOptions
            {
                Size = topK
            };

            searchOptions.VectorSearch = new()
            {
                Queries =
                {
                    new VectorizedQuery(queryEmbedding.ToArray())
                    {
                        KNearestNeighborsCount = topK,
                        Fields = { "ContentVector" }
                    }
                }
            };

            // Execute search
            SearchResults<SearchDocumentChunk> results = await _searchClient.SearchAsync<SearchDocumentChunk>(
                null,
                searchOptions,
                cancellationToken);

            var searchResults = new List<FinancialDocumentSearchResult>();

            await foreach (var result in results.GetResultsAsync())
            {
                searchResults.Add(new FinancialDocumentSearchResult
                {
                    Id = result.Document.Id,
                    Content = result.Document.Content,
                    SourceFileName = result.Document.SourceFileName,
                    SectionHeading = result.Document.SectionHeading,
                    RelevanceScore = result.Score ?? 0,
                    ChunkIndex = result.Document.ChunkIndex,
                    TotalChunks = result.Document.TotalChunks
                });
            }

            _logger.LogDebug("Found {ResultCount} documents", searchResults.Count);
            return searchResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during document search: {Query}", query);
            throw;
        }
    }

    public async Task<IReadOnlyList<FinancialDocumentSearchResult>> SearchDocumentsByKeywordAsync(
        string keywords,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(keywords))
            {
                return Array.Empty<FinancialDocumentSearchResult>();
            }

            _logger.LogDebug("Searching documents with keyword query: {Keywords}", keywords);

            // Perform keyword search
            var searchOptions = new SearchOptions
            {
                Size = topK,
                QueryType = SearchQueryType.Full
            };

            SearchResults<Models.SearchDocumentChunk> results = await _searchClient.SearchAsync<SearchDocumentChunk>(
                keywords,
                searchOptions,
                cancellationToken);

            var searchResults = new List<FinancialDocumentSearchResult>();

            await foreach (var result in results.GetResultsAsync())
            {
                searchResults.Add(new FinancialDocumentSearchResult
                {
                    Id = result.Document.Id,
                    Content = result.Document.Content,
                    SourceFileName = result.Document.SourceFileName,
                    SectionHeading = result.Document.SectionHeading,
                    RelevanceScore = result.Score ?? 0,
                    ChunkIndex = result.Document.ChunkIndex,
                    TotalChunks = result.Document.TotalChunks
                });
            }

            _logger.LogDebug("Found {ResultCount} documents", searchResults.Count);
            return searchResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during keyword search: {Keywords}", keywords);
            throw;
        }
    }
}
