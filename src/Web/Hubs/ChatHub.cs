using System.Text.RegularExpressions;
using AiAdvisor.Infrastructure.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace AiAdvisor.Web.Hubs;

[Authorize]
public class ChatHub(
    IAdvisorAgent advisorAgent,
    IMemoryCache memoryCache,
    ILogger<ChatHub> logger) : Hub
{
    private static readonly Regex ThinkBlock = new(@"<think>[\s\S]*?</think>", RegexOptions.Compiled);
    private const int MaxConversationHistory = 50; // Max messages to keep per user

    public async Task SendMessage(string message)
    {
        logger.LogInformation("User {UserId} sent a chat message", Context.UserIdentifier);

        var userId = Context.UserIdentifier ?? "anonymous";
        var cacheKey = $"chat_history_{userId}";

        // Get or create conversation history for this user
        if (!memoryCache.TryGetValue(cacheKey, out List<ConversationMessage>? conversationHistory))
        {
            conversationHistory = [];
            memoryCache.Set(cacheKey, conversationHistory, TimeSpan.FromHours(1));
        }

        // Add user message to history
        conversationHistory.Add(ConversationMessage.User(message));

        // Get AI response with full conversation context using both agents
        // Agent 1 (Financial Data): Builds personalized system prompt
        // Agent 2 (Advisor): Uses system prompt + chat service to provide advice
        var raw = await advisorAgent.GetAdviceAsync(userId, message, conversationHistory, Context.ConnectionAborted);
        var response = ThinkBlock.Replace(raw, string.Empty).Trim();

        // Add AI response to history
        conversationHistory.Add(ConversationMessage.Assistant(response));

        // Trim history if it gets too long
        if (conversationHistory.Count > MaxConversationHistory)
        {
            conversationHistory.RemoveRange(0, conversationHistory.Count - MaxConversationHistory);
        }

        // Update cache with new conversation history
        memoryCache.Set(cacheKey, conversationHistory, TimeSpan.FromHours(1));

        await Clients.Caller.SendAsync("ReceiveMessage", response);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        // Optional: Clean up conversation history on disconnect
        var userId = Context.UserIdentifier ?? "anonymous";
        var cacheKey = $"chat_history_{userId}";
        memoryCache.Remove(cacheKey);

        logger.LogInformation("User {UserId} disconnected", userId);
        return base.OnDisconnectedAsync(exception);
    }
}