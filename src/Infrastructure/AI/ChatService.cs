using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace AiAdvisor.Infrastructure.AI;

public interface IChatService
{
    Task<string> SendAsync(string message, CancellationToken ct);
    IAsyncEnumerable<string> StreamAsync(string message, CancellationToken ct);
}

public class ChatService : IChatService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<ChatService> _logger;

    private const string SystemPrompt = """
        You are a concise AI financial advisor. Always answer in 2-4 sentences or short bullet points.
        Use the following financial profile to give personalized advice:

        ACCOUNTS:
        - Current Account (GR16 0140 1250 **** 0000): €3,420.50
        - Savings Account (GR16 0140 1250 **** 0012): €12,750.00

        CARDS:
        - Visa Debit (**** 4521): €1,850 spent of €5,000 limit (37%)
        - Mastercard Credit (**** 9032): €620 spent of €3,000 limit (21%)

        MONTHLY EXPENSES BY CATEGORY:
        - Housing: €1,200 (48%)
        - Food: €450 (18%)
        - Transport: €280 (11%)
        - Utilities: €200 (8%)
        - Entertainment: €150 (6%)
        - Other: €120 (5%)
        - Total: €2,400

        RECENT TRANSACTIONS:
        - 10 May: Transfer to Savings (-€500.00)
        - 09 May: Supermarket – Sklavenitis S.A. (-€86.40) [Food]
        - 08 May: Electricity bill – DEI (-€94.00) [Utilities]
        - 07 May: Salary – May 2025 from Employer Ltd. (+€2,800.00)
        - 06 May: Netflix (-€14.99) [Entertainment]
        - 05 May: Fuel – Shell Station (-€65.20) [Transport]
        - 04 May: Rent – May 2025 (-€1,200.00) [Housing]
        - 03 May: Coffee & snack – Mikel Coffee (-€8.60) [Food]
        """;

    public ChatService(IChatClient chatClient, ILogger<ChatService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<string> SendAsync(string message, CancellationToken ct)
    {
        var messages = new[]
        {
            new ChatMessage(ChatRole.System, SystemPrompt),
            new ChatMessage(ChatRole.User, message)
        };

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);

        _logger.LogInformation("AI response — model: {Model}, tokens: {Tokens}",
            response.ModelId ?? "unknown",
            response.Usage?.TotalTokenCount);

        return response.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string message,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var messages = new[]
        {
            new ChatMessage(ChatRole.System, SystemPrompt),
            new ChatMessage(ChatRole.User, message)
        };

        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }
    }
}
