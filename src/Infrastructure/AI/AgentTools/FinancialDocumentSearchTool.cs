using AiAdvisor.Infrastructure.AI.Services;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.AI.Tools;

/// <summary>
/// Tool that allows the advisor agent to search financial documents on-demand
/// during conversation. This enables the agent to find relevant information
/// when the user asks specific questions about financial topics.
/// </summary>
public class FinancialDocumentSearchTool
{
    private readonly IFinancialDocumentSearchService _searchService;
    private readonly ILogger<FinancialDocumentSearchTool> _logger;

    public FinancialDocumentSearchTool(
        IFinancialDocumentSearchService searchService,
        ILogger<FinancialDocumentSearchTool> logger)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches financial documents for information related to the query.
    /// This method can be called by the AI agent to find relevant financial advice.
    /// </summary>
    /// <param name="query">The financial question or topic to search for</param>
    /// <param name="topK">Number of results to return (default 3)</param>
    /// <returns>Formatted search results as a string for the agent to use</returns>
    public async Task<string> SearchFinancialDocumentsAsync(string query, int topK = 3)
    {
        try
        {
            _logger.LogDebug("Agent requesting financial document search: {Query}", query);

            if (string.IsNullOrWhiteSpace(query))
            {
                return "No search query provided.";
            }

            var results = await _searchService.SearchDocumentsAsync(query, topK);

            if (results.Count == 0)
            {
                _logger.LogDebug("No results found for query: {Query}", query);
                return "No relevant financial documents found for this query.";
            }

            // Format results for agent consumption
            var formattedResults = FormatSearchResults(results);
            _logger.LogDebug("Found {ResultCount} relevant documents", results.Count);

            return formattedResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching financial documents: {Query}", query);
            return $"Error searching documents: {ex.Message}";
        }
    }

    /// <summary>
    /// Searches financial documents using keyword matching for more specific queries.
    /// </summary>
    public async Task<string> SearchFinancialDocumentsByKeywordAsync(string keywords, int topK = 3)
    {
        try
        {
            _logger.LogDebug("Agent requesting keyword search: {Keywords}", keywords);

            if (string.IsNullOrWhiteSpace(keywords))
            {
                return "No search keywords provided.";
            }

            var results = await _searchService.SearchDocumentsByKeywordAsync(keywords, topK);

            if (results.Count == 0)
            {
                return "No relevant financial documents found for these keywords.";
            }

            return FormatSearchResults(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents by keywords: {Keywords}", keywords);
            return $"Error searching documents: {ex.Message}";
        }
    }

    /// <summary>
    /// Formats search results into a readable string for the agent to include in its response.
    /// </summary>
    private static string FormatSearchResults(IReadOnlyList<FinancialDocumentSearchResult> results)
    {
        if (results.Count == 0)
            return "No results found.";

        var lines = new List<string> { "**Financial Reference Materials:**\n" };

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            lines.Add($"### {i + 1}. {result.SectionHeading} (from {result.SourceFileName})");
            lines.Add($"*Relevance: {(result.RelevanceScore * 100):F0}%*\n");
            lines.Add($"{result.Content}\n");
            lines.Add("---\n");
        }

        return string.Join(Environment.NewLine, lines);
    }
}
