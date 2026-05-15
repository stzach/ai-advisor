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

    public ChatService(IChatClient chatClient, ILogger<ChatService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<string> SendAsync(string message, CancellationToken ct)
    {
        var response = await _chatClient.GetResponseAsync(
            new ChatMessage(ChatRole.User, message),
            cancellationToken: ct);

        _logger.LogInformation("AI response — model: {Model}, tokens: {Tokens}",
            response.ModelId ?? "unknown",
            response.Usage?.TotalTokenCount);

        return response.Text ?? string.Empty;
    }

     public async IAsyncEnumerable<string> StreamAsync(
        string message,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var update in _chatClient.GetStreamingResponseAsync(
            new ChatMessage(ChatRole.User, message),
            cancellationToken: ct))
        {
            if (!string.IsNullOrEmpty(update.Text))
                yield return update.Text;
        }
    }
}
