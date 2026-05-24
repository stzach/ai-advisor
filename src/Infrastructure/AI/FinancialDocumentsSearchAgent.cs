using AiAdvisor.Application.Common.Interfaces;
using AiAdvisor.Infrastructure.AI.Services;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.AI;
public interface IFinancialDocumentsSearchAgent
{
    Task<string> GetSearchResultsAsync(string? userMessage, CancellationToken ct);
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

    public async Task<string> GetSearchResultsAsync(string? userMessage, CancellationToken cancellationToken = default)
    {
        // var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated.");

        // _logger.LogInformation("Generating search query for user {UserId}", userId);

        // var to  = DateTimeOffset.UtcNow;
        // var from = new DateTimeOffset(to.Year, to.Month, 1, 0, 0, 0, TimeSpan.Zero);
        // var financialContext = await _financialDataAgent.BuildUserSystemPromptAsync(userId, from, to, cancellationToken);

        var systemPrompt = """
        You are a Retrieval Query Builder Agent for a banking assistant system.

        Your task is to generate a single optimized semantic search query for retrieving the most relevant banking knowledge, policies, FAQs, financial guidance articles, and bank product information needed to answer the user’s latest message.

        The query will be used for embedding/vector search, so it must maximize semantic relevance, intent clarity, contextual completeness, and banking domain coverage.

        INPUTS
        You may receive:
        - Current user message
        - Conversation history
        - User profile
        - Existing bank products
        - Transaction behavior
        - Intent signals

        PRIMARY OBJECTIVE
        Generate a natural language retrieval query that captures:
        - The user’s immediate intent/question
        - Relevant banking and financial concepts
        - Context from prior conversation when useful
        - Relevant user financial situation when helpful
        - Possible related intents needed for a complete answer

        RETRIEVAL STRATEGY
        - Prioritize the latest user message over all other signals
        - Use conversation history to resolve ambiguity and maintain continuity
        - Include banking terminology likely to appear in support docs, FAQs, policies, product pages, or financial articles
        - Expand implicit intent into semantically related concepts
        - Include likely required knowledge areas, constraints, eligibility rules, fees, limits, risks, rates, or procedures
        - Infer missing but relevant financial context from behavior when appropriate
        - Optimize for retrieval breadth without becoming vague


        STRICT RULES
        - Output MUST be a single search query string only
        - Do NOT output JSON, markdown, labels, explanations, or multiple queries
        - Do NOT mention internal system fields or metadata
        - Do NOT include sensitive personal information
        - Do NOT answer the user directly
        - Do NOT generate conversational text
        - Keep it concise but information-dense (1–3 sentences max)

        GOOD QUERY CHARACTERISTICS
        - Natural language
        - Rich in financial semantics
        - Includes intent + context + banking terminology
        - Optimized for semantic retrieval
        - Specific enough for accurate retrieval
        - Broad enough to retrieve supporting documents

        EXAMPLE OUTPUTS

        customer asking why international card payment was declined while traveling abroad, possible fraud prevention blocks, travel notification requirements, foreign transaction limits, card security checks, and steps to re-enable overseas payments

        customer wants to increase credit card limit after salary increase, eligibility criteria, income requirements, credit assessment process, temporary versus permanent limit increases, and impact on credit profile

        customer asking about early loan repayment options, prepayment penalties, interest recalculation, partial repayment process, and ways to reduce total borrowing cost

        customer with frequent subscription and dining transactions looking for cashback or rewards credit cards with low annual fees, spending categories, and reward optimization benefits     
   """;

           _logger.LogInformation("System prompt built for SystemPrompt: \n{SystemPrompt}", systemPrompt);

    
        // var searchPrompt = userMessage ?? "";// ?? $"User financial data:\n\n{financialContext}\n\n Based on this information, generate a single optimized search query for retrieving relevant financial articles and bank product offers from a vector database. Focus on the user's financial intent and needs.";

        // _logger.LogInformation("Creating querry for \n\n Message: \n{UserMessage}", searchPrompt);

        // var response = await _chatService.SendAsync(searchPrompt, systemPrompt, cancellationToken);

        // _logger.LogInformation("Received financial document search query response Response: \n{Response}", response);

        return Search(userMessage ?? "");
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