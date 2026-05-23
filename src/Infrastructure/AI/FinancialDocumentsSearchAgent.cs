using AiAdvisor.Application.Common.Interfaces;
using AiAdvisor.Infrastructure.AI.Services;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.AI;
public interface IFinancialDocumentsSearchAgent
{
    Task<string> GetSearchResultsAsync(CancellationToken ct);
}
public class FinancialDocumentsSearchAgent : IFinancialDocumentsSearchAgent
{
    private readonly IFinancialService _financialDataAgent;
    private readonly IChatService _chatService;

    private readonly IFinancialDocumentSearchService _financialDocumentSearchService;
    private readonly IUser _user;
    private readonly ILogger<FinancialDocumentsSearchAgent> _logger;


    public FinancialDocumentsSearchAgent(
        IFinancialService financialDataAgent,
        IFinancialDocumentSearchService financialDocumentSearchService,
        IChatService chatService,
        IUser user,
        ILogger<FinancialDocumentsSearchAgent> logger)
    {
        _financialDataAgent = financialDataAgent;
        _financialDocumentSearchService = financialDocumentSearchService;
        _chatService        = chatService;
        _user               = user;
        _logger             = logger;
    }

    public async Task<string> GetSearchResultsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated.");

        _logger.LogInformation("Generating search query for user {UserId}", userId);

        var to  = DateTimeOffset.UtcNow;
        var from = new DateTimeOffset(to.Year, to.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var financialContext = await _financialDataAgent.BuildUserSystemPromptAsync(userId, from, to, cancellationToken);

        var systemPrompt = """
        You are a Retrieval Query Builder Agent for a banking assistant system.

        Your job is to convert structured user data (user profile, transaction history, existing bank products, and optional intent signals) into a single optimized semantic search query for a vector database containing financial articles and bank product offers.

        This query will be used for embedding-based retrieval, so it must maximize semantic relevance, intent clarity, and financial domain coverage.

        INSTRUCTIONS
        - Infer user intent from behavior (spending patterns, income, savings, investments, loans, subscriptions, etc.)
        - Translate both explicit and inferred needs into a natural language search query
        - Focus on financial intent such as saving, investing, borrowing, insurance, travel, business banking, budgeting, and wealth management
        - Combine context + intent + financial concepts into a single coherent query
        - Prioritize recent behavioral signals over static profile data
        - Make the query suitable for semantic (vector) search across both articles and bank offers

        STRICT RULES
        - Output MUST be a single string only
        - Do NOT output JSON, markdown, labels, or explanations
        - Do NOT include user identifiers or sensitive data
        - Do NOT mention “user profile”, “transactions”, or internal system fields
        - Do NOT generate multiple queries
        - Keep it concise (1–3 sentences max)

        OUTPUT FORMAT
        Return ONLY the final search query string.

        EXAMPLE OUTPUTS

        young professional with moderate income seeking credit card rewards for travel and everyday spending, interested in cashback benefits, low fees, and building credit history

        user with high savings balance looking for fixed deposit accounts, premium savings options, and low risk investment products with stable returns

        small business owner with frequent transactions needing business account, credit line for cash flow management, and expense tracking tools
        """;

        var userMessage = $"User financial data:\n\n{financialContext}\n\n Based on this information, generate a single optimized search query for retrieving relevant financial articles and bank product offers from a vector database. Focus on the user's financial intent and needs.";

        var response = await _chatService.SendAsync(userMessage, systemPrompt, cancellationToken);

        _logger.LogInformation("Received financial document search query response for user {UserId} \n\n Response: \n{Response}", userId, response);

        return Search(response);
    }

    private string Search(string response)
    {
        try
        {
            var query = response.Trim();
            var results = _financialDocumentSearchService.SearchDocumentsAsync(query, topK: 5).Result;
          var formattedResults = FormatSearchResults(results);
            _logger.LogInformation("Search completed with {ResultCount} results \n\n Content: \n{Content}", results.Count, formattedResults);
            return "" ;//formattedResults;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse search query from AI response");
            return "";
        }
    }

    private string FormatSearchResults(IReadOnlyList<FinancialDocumentSearchResult> results)
    {
        if (results.Count == 0)
            return "No relevant documents found.";

        var lines = new List<string> { "**Financial Reference Materials:**\n" };

        foreach (var result in results)
        {
            lines.Add($"- **{result.SourceFileName}** (Section: {result.SectionHeading}, Relevance: {result.RelevanceScore:F2})");
            lines.Add($"  {result.Content}");
        }

        return string.Join("\n", lines);
    }

}