using Microsoft.Extensions.AI;

namespace AiAdvisor.Infrastructure.AI;

public interface IChatService
{
    Task<string> SendAsync(string message, CancellationToken ct);
}

public class ChatService : IChatService
{
    private readonly IChatClient _chatClient;

    public ChatService(IChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<string> SendAsync(string message, CancellationToken ct)
    {
        var response = await _chatClient.GetResponseAsync(
            new ChatMessage(ChatRole.User, message),
            cancellationToken: ct);
        return response.Text ?? string.Empty;
    }
}
