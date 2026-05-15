using System.Text.RegularExpressions;
using AiAdvisor.Infrastructure.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AiAdvisor.Web.Hubs;

[Authorize]
public class ChatHub(IChatService chatService, ILogger<ChatHub> logger) : Hub
{
    private static readonly Regex ThinkBlock = new(@"<think>[\s\S]*?</think>", RegexOptions.Compiled);

    public async Task SendMessage(string message)
    {
        logger.LogInformation("User {UserId} sent a chat message", Context.UserIdentifier);

        var raw = await chatService.SendAsync(message, Context.ConnectionAborted);
        var response = ThinkBlock.Replace(raw, string.Empty).Trim();
        await Clients.Caller.SendAsync("ReceiveMessage", response);
    }
}