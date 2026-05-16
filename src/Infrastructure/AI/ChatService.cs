using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace AiAdvisor.Infrastructure.AI;

public interface IChatService
{
    Task<string> SendAsync(string message, string systemPrompt, CancellationToken ct);
    Task<string> SendAsync(string message, string systemPrompt, List<ConversationMessage> conversationHistory, CancellationToken ct);
    IAsyncEnumerable<string> StreamAsync(string message, string systemPrompt, CancellationToken ct);
    IAsyncEnumerable<string> StreamAsync(string message, string systemPrompt, List<ConversationMessage> conversationHistory, CancellationToken ct);
}

public class ChatService : IChatService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<ChatService> _logger;

    public ChatService(IChatClient chatClient, ILogger<ChatService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<string> SendAsync(string message, string systemPrompt, CancellationToken ct)
    {
        return await SendAsync(message, systemPrompt, [], ct);
    }

    public async Task<string> SendAsync(string message, string systemPrompt, List<ConversationMessage> conversationHistory, CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, systemPrompt)
        };

        // Add previous conversation history
        foreach (var entry in conversationHistory)
        {
            var role = entry.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? ChatRole.User : ChatRole.Assistant;
            messages.Add(new ChatMessage(role, entry.Content));
        }

        // Add the current message
        messages.Add(new ChatMessage(ChatRole.User, message));

        var response = await _chatClient.GetResponseAsync(messages, cancellationToken: ct);

        _logger.LogInformation("AI response — model: {Model}, tokens: {Tokens}",
            response.ModelId ?? "unknown",
            response.Usage?.TotalTokenCount);

        return response.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string message,
        string systemPrompt,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var chunk in StreamAsync(message, systemPrompt, [], ct))
            yield return chunk;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string message,
        string systemPrompt,
        List<ConversationMessage> conversationHistory,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var messages = new List<ChatMessage>
        {
            new ChatMessage(ChatRole.System, systemPrompt)
        };

        // Add previous conversation history
        foreach (var entry in conversationHistory)
        {
            var role = entry.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? ChatRole.User : ChatRole.Assistant;
            messages.Add(new ChatMessage(role, entry.Content));
        }

        // Add the current message
        messages.Add(new ChatMessage(ChatRole.User, message));

        await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, cancellationToken: ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }
    }
}
