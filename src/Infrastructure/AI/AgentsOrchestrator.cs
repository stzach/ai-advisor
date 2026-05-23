using System.Runtime.CompilerServices;
using AiAdvisor.Application.AiInsights.Queries.GetAiInsights;
using AiAdvisor.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.AI;

/// <summary>
/// Orchestrates multiple AI agents for different use cases (chat, insights, etc.)
/// Provides a unified interface to call configured agents with optional document search context
/// </summary>
public interface IAgentsOrchestrator
{
    /// <summary>
    /// Get advisor advice with optional document search enrichment
    /// </summary>
    Task<string> GetAdviceAsync(
        string userId,
        string message,
        List<ConversationMessage> conversationHistory,
        CancellationToken ct);

    /// <summary>
    /// Stream advisor advice with optional document search enrichment
    /// </summary>
    IAsyncEnumerable<string> StreamAdviceAsync(
        string userId,
        string message,
        List<ConversationMessage> conversationHistory,
        CancellationToken ct);

    /// <summary>
    /// Get AI-generated insights with optional document search context
    /// </summary>
    Task<List<InsightDto>> GetInsightsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>
    /// Stream AI-generated insights with optional document search context
    /// </summary>
    IAsyncEnumerable<InsightDto> StreamInsightsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);

    /// <summary>
    /// Get search results from financial documents
    /// </summary>
    Task<string> GetDocumentSearchResultsAsync(CancellationToken ct);
}

/// <summary>
/// Implementation of the agents orchestrator
/// Coordinates between AdvisorAgent, InsightsAgent, and FinancialDocumentsSearchAgent
/// </summary>
public class AgentsOrchestrator : IAgentsOrchestrator
{
    private readonly IAdvisorAgent _advisorAgent;
    private readonly IInsightsAgent _insightsAgent;
    private readonly IFinancialDocumentsSearchAgent _financialSearchAgent;
    private readonly ILogger<AgentsOrchestrator> _logger;

    public AgentsOrchestrator(
        IAdvisorAgent advisorAgent,
        IInsightsAgent insightsAgent,
        IFinancialDocumentsSearchAgent financialSearchAgent,
        ILogger<AgentsOrchestrator> logger)
    {
        _advisorAgent = advisorAgent ?? throw new ArgumentNullException(nameof(advisorAgent));
        _insightsAgent = insightsAgent ?? throw new ArgumentNullException(nameof(insightsAgent));
        _financialSearchAgent = financialSearchAgent ?? throw new ArgumentNullException(nameof(financialSearchAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get advisor advice with optional document search enrichment for chat scenarios
    /// </summary>
    public async Task<string> GetAdviceAsync(
        string userId,
        string message,
        List<ConversationMessage> conversationHistory,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Orchestrator: Getting advice from AdvisorAgent for user {UserId}", userId);

            // Get base advice from advisor agent
            var advice = await _advisorAgent.GetAdviceAsync(userId, message, conversationHistory, ct);

            // Enrich with document search
            _logger.LogInformation("Orchestrator: Enriching advice with document search for user {UserId}", userId);
            try
            {
                var searchResults = await _financialSearchAgent.GetSearchResultsAsync(ct);
                advice = EnrichAdviceWithSearchResults(advice, searchResults);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Document search enrichment failed for user {UserId}. Returning base advice", userId);
            }

            return advice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAdviceAsync for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Stream advisor advice with optional document search enrichment
    /// </summary>
    public async IAsyncEnumerable<string> StreamAdviceAsync(
        string userId,
        string message,
        List<ConversationMessage> conversationHistory,
        [EnumeratorCancellation] CancellationToken ct)
    {
        _logger.LogInformation("Orchestrator: Streaming advice from AdvisorAgent for user {UserId}", userId);

        // Stream base advice
        await foreach (var chunk in _advisorAgent.StreamAdviceAsync(userId, message, conversationHistory, ct))
        {
            yield return chunk;
        }

        // Enrich with document search
        var searchResults = string.Empty;
        try
        {
            searchResults = await _financialSearchAgent.GetSearchResultsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Document search enrichment failed during streaming for user {UserId}", userId);
        }

        if (!string.IsNullOrEmpty(searchResults))
        {
            var enrichmentSeparator = "\n\n**Relevant Resources:**\n";
            yield return enrichmentSeparator + searchResults;
        }
    }

    /// <summary>
    /// Get AI-generated insights with optional document search context
    /// </summary>
    public async Task<List<InsightDto>> GetInsightsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Orchestrator: Getting insights from InsightsAgent");

            // Get base insights
            var insights = await _insightsAgent.GetInsightsAsync(from, to, ct);

            // Augment with document search context
            _logger.LogInformation("Orchestrator: Augmenting insights with document search context");
            try
            {
                var searchResults = await _financialSearchAgent.GetSearchResultsAsync(ct);
                _logger.LogDebug("Document search context retrieved for insights: {ResultLength} chars", searchResults?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Document search context failed for insights. Continuing with base insights.");
            }

            return insights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetInsightsAsync");
            throw;
        }
    }

    /// <summary>
    /// Stream AI-generated insights with optional document search context
    /// </summary>
    public async IAsyncEnumerable<InsightDto> StreamInsightsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        [EnumeratorCancellation] CancellationToken ct)
    {
        _logger.LogInformation("Orchestrator: Streaming insights from InsightsAgent");

        // Stream base insights
        await foreach (var insight in _insightsAgent.StreamInsightsAsync(from, to, ct))
        {
            yield return insight;
        }

        // Document search context retrieved in parallel for UI consumption
        try
        {
            var searchResults = await _financialSearchAgent.GetSearchResultsAsync(ct);
            _logger.LogDebug("Document search context available for insights stream");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Document search failed during insights stream");
        }
    }

    /// <summary>
    /// Get search results from financial documents
    /// </summary>
    public async Task<string> GetDocumentSearchResultsAsync(CancellationToken ct)
    {
        return await _financialSearchAgent.GetSearchResultsAsync(ct);
    }

    // ============== Private Helper Methods ==============

    private static string EnrichAdviceWithSearchResults(string baseAdvice, string searchResults)
    {
        if (string.IsNullOrEmpty(searchResults))
            return baseAdvice;

        return $"{baseAdvice}\n\n**Relevant Resources:**\n{searchResults}";
    }
}
