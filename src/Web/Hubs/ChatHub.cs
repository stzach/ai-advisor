using System.Text.RegularExpressions;
using AiAdvisor.Infrastructure.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace AiAdvisor.Web.Hubs;

[Authorize]
public class ChatHub(
    IAdvisorAgent agentsOrchestrator,
    IMemoryCache memoryCache,
    ILogger<ChatHub> logger) : Hub
{
    private static readonly Regex ThinkBlock = new(@"<think>[\s\S]*?</think>", RegexOptions.Compiled);
    private const int MaxConversationHistory = 50;

    /// <summary>
    /// Define which agents to use for chat - can be customized per message
    /// </summary>
    private static readonly AgentPipeline DefaultChatPipeline = AgentPipeline.ChatPipeline;

    public async Task SendMessage(string message)
    {
        logger.LogInformation("User {UserId} sent a chat message", Context.UserIdentifier);

        var userId = Context.UserIdentifier ?? "anonymous";
        var cacheKey = $"chat_history_{userId}";

        if (!memoryCache.TryGetValue(cacheKey, out List<ConversationMessage>? conversationHistory))
        {
            conversationHistory = [];
            memoryCache.Set(cacheKey, conversationHistory, TimeSpan.FromHours(1));
        }

        conversationHistory.Add(ConversationMessage.User(message));

        // Stream tokens to client as they arrive
        var rawBuffer = new System.Text.StringBuilder();
        await foreach (var chunk in agentsOrchestrator.StreamAdviceAsync(
            userId, message, conversationHistory, Context.ConnectionAborted))
        {
            rawBuffer.Append(chunk);
            await Clients.Caller.SendAsync("ReceiveChunk", chunk, Context.ConnectionAborted);
        }

        var response = ThinkBlock.Replace(rawBuffer.ToString(), string.Empty).Trim();

        // Signal end of stream so client can finalise the message
        await Clients.Caller.SendAsync("ReceiveChunkDone", response, Context.ConnectionAborted);

        conversationHistory.Add(ConversationMessage.Assistant(response));

        if (conversationHistory.Count > MaxConversationHistory)
            conversationHistory.RemoveRange(0, conversationHistory.Count - MaxConversationHistory);

        memoryCache.Set(cacheKey, conversationHistory, TimeSpan.FromHours(1));
    }

    /// <summary>
    /// Optional: Allow custom agent pipeline selection from client
    /// </summary>
    public async Task SendMessageWithPipeline(string message, AgentType[] agentTypes)
    {
        var pipeline = new AgentPipeline(agentTypes);
        
        logger.LogInformation("User {UserId} sent message with custom pipeline: {Agents}", Context.UserIdentifier, string.Join(",", agentTypes));

        var userId = Context.UserIdentifier ?? "anonymous";
        var cacheKey = $"chat_history_{userId}";

        if (!memoryCache.TryGetValue(cacheKey, out List<ConversationMessage>? conversationHistory))
        {
            conversationHistory = [];
            memoryCache.Set(cacheKey, conversationHistory, TimeSpan.FromHours(1));
        }

        conversationHistory.Add(ConversationMessage.User(message));

        var raw = await agentsOrchestrator.GetAdviceAsync(
            userId,
            message,
            conversationHistory,
            Context.ConnectionAborted);

        var response = ThinkBlock.Replace(raw, string.Empty).Trim();

        conversationHistory.Add(ConversationMessage.Assistant(response));

        if (conversationHistory.Count > MaxConversationHistory)
        {
            conversationHistory.RemoveRange(0, conversationHistory.Count - MaxConversationHistory);
        }

        memoryCache.Set(cacheKey, conversationHistory, TimeSpan.FromHours(1));

        await Clients.Caller.SendAsync("ReceiveMessage", response);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? "anonymous";
        var cacheKey = $"chat_history_{userId}";
        memoryCache.Remove(cacheKey);

        logger.LogInformation("User {UserId} disconnected", userId);
        return base.OnDisconnectedAsync(exception);
    }
}