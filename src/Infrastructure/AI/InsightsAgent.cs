using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiAdvisor.Application.AiInsights.Queries.GetAiInsights;
using AiAdvisor.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.AI;

public class InsightsAgent : IInsightsAgent
{
    private readonly IFinancialService _financialDataAgent;
    private readonly IChatService _chatService;
    private readonly IUser _user;
    private readonly ILogger<InsightsAgent> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public InsightsAgent(
        IFinancialService financialDataAgent,
        IChatService chatService,
        IUser user,
        ILogger<InsightsAgent> logger)
    {
        _financialDataAgent = financialDataAgent;
        _chatService        = chatService;
        _user               = user;
        _logger             = logger;
    }

    public async Task<List<InsightDto>> GetInsightsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated.");

        _logger.LogInformation("Generating AI insights for user {UserId}", userId);

        var financialContext = await _financialDataAgent.BuildUserSystemPromptAsync(userId, from, to, cancellationToken);
        var (systemPrompt, userMessage) = BuildPrompt(financialContext);

        var response = await _chatService.SendAsync(userMessage, systemPrompt, cancellationToken);

        _logger.LogInformation("Received insights response \n\n Response: \n{Response}", response);

        return ParseInsights(response);
    }

    public async IAsyncEnumerable<InsightDto> StreamInsightsAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated.");

        _logger.LogInformation("Streaming AI insights for user {UserId}", userId);

        var financialContext = await _financialDataAgent.BuildUserSystemPromptAsync(userId, from, to, cancellationToken);
        var (systemPrompt, userMessage) = BuildPrompt(financialContext);
        var tokenStream = _chatService.StreamAsync(userMessage, systemPrompt, cancellationToken);

        await foreach (var insight in ParseStreamingInsightsAsync(tokenStream, cancellationToken))
            yield return insight;
    }

    private static (string systemPrompt, string userMessage) BuildPrompt(string financialContext)
    {
        var systemPrompt = """
            You are a financial advisor AI for a retail bank. Analyse the user's financial data and return exactly 4 personalised, actionable insights.

            CRITICAL CONSTRAINT — Bank-actionable only:
            Every insight, CTA, and prompt MUST map to an action the user can take *within the bank's app or product catalogue*. Do NOT suggest lifestyle changes, behavioural advice, or anything the bank cannot execute on the user's behalf.

            Allowed action categories (bank features the user can act on):
            - Set/adjust spending limits or category budgets
            - Enable transaction alerts or notifications
            - Move funds between accounts (savings, deposits, sub-accounts)
            - Open a new product (savings account, term deposit, investment fund, credit card)
            - Set up standing orders, direct debits, or automated transfers
            - Round-up / auto-save rules
            - Consolidate or refinance existing debt with the bank
            - Switch card / plan tiers
            - Activate cashback, rewards, or partner offers
            - Review and cancel recurring subscriptions detected by the bank
            - Apply for an overdraft adjustment or loan

            Forbidden suggestions (these are NOT bank actions):
            - "Cook at home", "eat out less", "use public transport", "shop around"
            - Any generic budgeting tip the user must execute themselves outside the app
            - Negotiating with third parties (landlords, providers) unless the bank offers a switching service
            - Vague advice like "be mindful of spending"

            Rules:
            - Return ONLY a valid JSON array — no markdown, no explanation, no code fences.
            - Base every insight on the actual figures in the data (mention real amounts or percentages where relevant).
            - The "prompt" field must be a question whose answer is a bank action (e.g. "How do I set a monthly food spending limit?" — NOT "How can I spend less on food?").
            - The "cta" must reference a concrete in-app destination (e.g. "Set spending limit", "Open savings", "Enable alerts").

            Each object must have exactly these fields:
                "icon"    : a single emoji relevant to the insight
                "message" : a concise insight grounded in the data (max 120 characters)
                "cta"     : a short in-app action label (2–5 words)
                "prompt"  : a follow-up question whose answer is a bank-provided action

            Example output:
            [
            {"icon":"🍽️","message":"You spent 34% of your income (€680) on food last month. A category limit can cap it.","cta":"Set food limit","prompt":"How do I set a monthly limit on food spending?"},
            {"icon":"🏦","message":"€500 sitting idle in your current account could earn ~€18/yr in our 3.6% savings account.","cta":"Open savings","prompt":"Which savings account fits my balance?"},
            {"icon":"🔔","message":"Utility bills rose €42 vs last month. Bill alerts can flag spikes earlier.","cta":"Enable bill alerts","prompt":"How do I turn on bill-increase alerts?"},
            {"icon":"💳","message":"You have €1,200 unused credit. Consolidating onto one card could lower fees.","cta":"View consolidation","prompt":"Can I consolidate my cards with the bank?"}
            ]
            """;

        var userMessage = $"Here is my financial data:\n\n{financialContext}\n\nGenerate 4 personalised insights.";
        return (systemPrompt, userMessage);
    }

    private async IAsyncEnumerable<InsightDto> ParseStreamingInsightsAsync(
        IAsyncEnumerable<string> tokenStream,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var buffer = new StringBuilder();

        await foreach (var chunk in tokenStream.WithCancellation(ct))
        {
            buffer.Append(chunk);

            while (TryExtractObject(buffer, out var json))
            {
                var insight = TryParseInsight(json);
                if (insight is not null) yield return insight;
            }
        }
    }

    private static bool TryExtractObject(StringBuilder sb, out string json)
    {
        json = "";
        var text = sb.ToString();
        var start = text.IndexOf('{');
        if (start < 0) { sb.Clear(); return false; }

        int depth = 0;
        bool inString = false, escaped = false;

        for (int i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (escaped)              { escaped = false; continue; }
            if (c == '\\' && inString){ escaped = true;  continue; }
            if (c == '"')             { inString = !inString; continue; }
            if (inString)             continue;

            if      (c == '{') depth++;
            else if (c == '}' && --depth == 0)
            {
                json = text[start..(i + 1)];
                sb.Remove(0, i + 1);
                return true;
            }
        }

        if (start > 0) sb.Remove(0, start);
        return false;
    }

    private InsightDto? TryParseInsight(string json)
    {
        try   { return JsonSerializer.Deserialize<InsightDto>(json, JsonOptions); }
        catch { return null; }
    }

    private List<InsightDto> ParseInsights(string response)
    {
        try
        {
            var json = response.Trim();

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
