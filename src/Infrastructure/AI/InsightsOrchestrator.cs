using System.Text.Json;
using System.Text.Json.Serialization;
using AiAdvisor.Application.AiInsights.Queries.GetAiInsights;
using AiAdvisor.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.AI;

public class InsightsOrchestrator : IInsightsOrchestrator
{
    private readonly IFinancialDataAgent _financialDataAgent;
    private readonly IChatService _chatService;
    private readonly IUser _user;
    private readonly ILogger<InsightsOrchestrator> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public InsightsOrchestrator(
        IFinancialDataAgent financialDataAgent,
        IChatService chatService,
        IUser user,
        ILogger<InsightsOrchestrator> logger)
    {
        _financialDataAgent = financialDataAgent;
        _chatService        = chatService;
        _user               = user;
        _logger             = logger;
    }

    public async Task<List<InsightDto>> GetInsightsAsync(CancellationToken cancellationToken = default)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated.");

        _logger.LogInformation("Generating AI insights for user {UserId}", userId);

        var financialContext = await _financialDataAgent.BuildUserSystemPromptAsync(userId, cancellationToken);

        var systemPrompt = """
            You are a financial advisor AI. Analyse the user's financial data and return exactly 4 personalised, actionable insights.

            Rules:
            - Return ONLY a valid JSON array — no markdown, no explanation, no code fences.
            - Base every insight on the actual figures in the data (mention real amounts or percentages where relevant).
            - Each object must have exactly these fields:
                "icon"    : a single emoji relevant to the insight
                "message" : a concise insight (max 120 characters)
                "cta"     : a short call-to-action label (2–5 words)
                "prompt"  : a follow-up question the user can ask the AI advisor

            Example output:
            [
              {"icon":"💡","message":"You spent 34% of your income on food last month — above average.","cta":"See breakdown","prompt":"How can I reduce my food spending?"},
              {"icon":"🏦","message":"Moving €500 to savings could earn you €18/year in interest.","cta":"Open savings","prompt":"What savings account would suit me?"},
              {"icon":"📊","message":"Your utility bills went up €42 compared to last month.","cta":"Explore options","prompt":"How can I reduce my utility costs?"},
              {"icon":"💳","message":"You have €1,200 unused credit across your cards.","cta":"View card offers","prompt":"Should I consolidate my credit cards?"}
            ]
            """;

        var userMessage = $"Here is my financial data:\n\n{financialContext}\n\nGenerate 4 personalised insights.";

        var response = await _chatService.SendAsync(userMessage, systemPrompt, cancellationToken);

        _logger.LogInformation("Received insights response for user {UserId}", userId);

        return ParseInsights(response);
    }

    private List<InsightDto> ParseInsights(string response)
    {
        try
        {
            var json = response.Trim();

            // Strip markdown code fences if the model wraps the JSON
            if (json.StartsWith("```"))
            {
                var start = json.IndexOf('\n') + 1;
                var end   = json.LastIndexOf("```");
                if (end > start)
                    json = json[start..end].Trim();
            }

            var insights = JsonSerializer.Deserialize<List<InsightDto>>(json, JsonOptions);
            return insights ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse insights JSON from AI response");
            return [];
        }
    }
}
