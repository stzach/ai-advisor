using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.AI;

/// <summary>
/// Agent 2: Advisor agent that orchestrates FinancialDataAgent + ChatService
/// Handles agent-to-agent communication and caching of system prompts
/// </summary>
public interface IAdvisorAgent
{
    Task<string> GetAdviceAsync(string userId, string message, List<ConversationMessage> conversationHistory, CancellationToken ct);
    IAsyncEnumerable<string> StreamAdviceAsync(string userId, string message, List<ConversationMessage> conversationHistory, CancellationToken ct);
}

public class AdvisorAgent : IAdvisorAgent
{
    private readonly IFinancialDataAgent _financialDataAgent;
    private readonly IChatService _chatService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<AdvisorAgent> _logger;

    private const string SystemPromptCacheKeyTemplate = "system_prompt_{0}";
    private const int SystemPromptCacheHours = 1;

    public AdvisorAgent(
        IFinancialDataAgent financialDataAgent,
        IChatService chatService,
        IMemoryCache memoryCache,
        ILogger<AdvisorAgent> logger)
    {
        _financialDataAgent = financialDataAgent;
        _chatService = chatService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <summary>
    /// Gets financial advice for a user
    /// Flow: Agent 1 (Financial Data) → Agent 2 (Advisor/Chat)
    /// </summary>
    public async Task<string> GetAdviceAsync(
        string userId,
        string message,
        List<ConversationMessage> conversationHistory,
        CancellationToken ct)
    {
        var systemPrompt = await GetOrBuildSystemPromptAsync(userId, ct);
        var advice = await _chatService.SendAsync(message, systemPrompt, conversationHistory, ct);
        return advice;
    }

    /// <summary>
    /// Streams financial advice for a user
    /// Flow: Agent 1 (Financial Data) → Agent 2 (Advisor/Chat with streaming)
    /// </summary>
    public async IAsyncEnumerable<string> StreamAdviceAsync(
        string userId,
        string message,
        List<ConversationMessage> conversationHistory,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var systemPrompt = await GetOrBuildSystemPromptAsync(userId, ct);
        await foreach (var chunk in _chatService.StreamAsync(message, systemPrompt, conversationHistory, ct))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Gets or builds the system prompt for a user (cached)
    /// </summary>
    private async Task<string> GetOrBuildSystemPromptAsync(string userId, CancellationToken ct)
    {
        var cacheKey = string.Format(SystemPromptCacheKeyTemplate, userId);

        if (_memoryCache.TryGetValue(cacheKey, out string? cachedPrompt))
        {
            _logger.LogInformation("Using cached system prompt for user {UserId}", userId);
            return cachedPrompt!;
        }

        _logger.LogInformation("Building system prompt for user {UserId}", userId);
        var systemPrompt = await _financialDataAgent.BuildUserSystemPromptAsync(userId, ct);

        // Cache the system prompt
        _memoryCache.Set(
            cacheKey,
            systemPrompt,
            TimeSpan.FromHours(SystemPromptCacheHours));

        return systemPrompt;
    }

    /// <summary>
    /// Invalidate cached system prompt for a user (call when user profile changes)
    /// </summary>
    public void InvalidateUserPromptCache(string userId)
    {
        var cacheKey = string.Format(SystemPromptCacheKeyTemplate, userId);
        _memoryCache.Remove(cacheKey);
        _logger.LogInformation("Invalidated system prompt cache for user {UserId}", userId);
    }
}
