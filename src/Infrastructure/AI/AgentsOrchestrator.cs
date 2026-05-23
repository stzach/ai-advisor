// using System.Runtime.CompilerServices;
// using System.Text.Json;
// using AiAdvisor.Application.Common.Interfaces;
// using Microsoft.Extensions.Logging;

// namespace AiAdvisor.Infrastructure.AI;

// /// <summary>
// /// Orchestrates multiple AI agents via an <see cref="AgentPipeline"/>.
// /// All advisor/insights/search flows are expressed as pipelines; callers
// /// pick a pipeline definition and interpret the resulting string output.
// /// </summary>
// public interface IAgentsOrchestrator
// {
//     Task<string> ExecuteAgentPipelineAsync(
//         string userId,
//         string message,
//         List<ConversationMessage> conversationHistory,
//         AgentPipeline pipeline,
//         CancellationToken ct,
//         DateTimeOffset? from = null,
//         DateTimeOffset? to = null);

//     IAsyncEnumerable<string> StreamAgentPipelineAsync(
//         string userId,
//         string message,
//         List<ConversationMessage> conversationHistory,
//         AgentPipeline pipeline,
//         CancellationToken ct,
//         DateTimeOffset? from = null,
//         DateTimeOffset? to = null);

//     Task<string> GetDocumentSearchResultsAsync(CancellationToken ct);
// }

// public class AgentsOrchestrator : IAgentsOrchestrator
// {
//     private readonly IAdvisorAgent _advisorAgent;
//     private readonly IInsightsAgent _insightsAgent;
//     private readonly IProductRecomendationAgent _productRecomendationAgent;
//     private readonly IFinancialDocumentsSearchAgent _financialSearchAgent;
//     private readonly ILogger<AgentsOrchestrator> _logger;

//     private static readonly JsonSerializerOptions JsonOptions = new()
//     {
//         PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//     };

//     public AgentsOrchestrator(
//         IAdvisorAgent advisorAgent,
//         IInsightsAgent insightsAgent,
//         IProductRecomendationAgent productRecomendationAgent,
//         IFinancialDocumentsSearchAgent financialSearchAgent,
//         ILogger<AgentsOrchestrator> logger)
//     {
//         _advisorAgent = advisorAgent ?? throw new ArgumentNullException(nameof(advisorAgent));
//         _insightsAgent = insightsAgent ?? throw new ArgumentNullException(nameof(insightsAgent));
//         _productRecomendationAgent = productRecomendationAgent ?? throw new ArgumentNullException(nameof(productRecomendationAgent));
//         _financialSearchAgent = financialSearchAgent ?? throw new ArgumentNullException(nameof(financialSearchAgent));
//         _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//     }

//     public async Task<string> ExecuteAgentPipelineAsync(
//         string userId,
//         string message,
//         List<ConversationMessage> conversationHistory,
//         AgentPipeline pipeline,
//         CancellationToken ct,
//         DateTimeOffset? from = null,
//         DateTimeOffset? to = null)
//     {
//         if (pipeline?.Agents == null || pipeline.Agents.Count == 0)
//             throw new ArgumentException("Pipeline must contain at least one agent", nameof(pipeline));

//         _logger.LogInformation("Orchestrator: Executing pipeline with {AgentCount} agents for user {UserId}", pipeline.Agents.Count, userId);

//         var pipelineResult = message;

//         foreach (var agentType in pipeline.Agents)
//         {
//             _logger.LogInformation("Orchestrator: Executing {AgentType} in pipeline for user {UserId}", agentType, userId);
//             pipelineResult = await ExecuteAgentAsync(userId, pipelineResult, conversationHistory, agentType, from, to, ct);
//         }

//         _logger.LogInformation("Orchestrator: Pipeline execution completed for user {UserId}", userId);
//         return pipelineResult;
//     }

//     public async IAsyncEnumerable<string> StreamAgentPipelineAsync(
//         string userId,
//         string message,
//         List<ConversationMessage> conversationHistory,
//         AgentPipeline pipeline,
//         [EnumeratorCancellation] CancellationToken ct,
//         DateTimeOffset? from = null,
//         DateTimeOffset? to = null)
//     {
//         if (pipeline?.Agents == null || pipeline.Agents.Count == 0)
//             throw new ArgumentException("Pipeline must contain at least one agent", nameof(pipeline));

//         _logger.LogInformation("Orchestrator: Streaming pipeline with {AgentCount} agents for user {UserId}", pipeline.Agents.Count, userId);

//         var pipelineResult = message;

//         for (var i = 0; i < pipeline.Agents.Count; i++)
//         {
//             var agentType = pipeline.Agents[i];
//             var isLast = i == pipeline.Agents.Count - 1;

//             _logger.LogInformation("Orchestrator: Streaming {AgentType} in pipeline for user {UserId}", agentType, userId);

//             switch (agentType)
//             {
//                 case AgentType.AdvisorAgent when isLast:
//                     await foreach (var chunk in _advisorAgent.StreamAdviceAsync(userId, pipelineResult, conversationHistory, ct))
//                         yield return chunk;
//                     break;

//                 case AgentType.InsightsAgent:
//                     var collectedInsights = new System.Text.StringBuilder("[");
//                     var firstInsight = true;
//                     await foreach (var insight in _insightsAgent.StreamInsightsAsync(from ?? DefaultFrom(), to ?? DefaultTo(), ct))
//                     {
//                         var json = JsonSerializer.Serialize(insight, JsonOptions);
//                         yield return json;
//                         if (!firstInsight) collectedInsights.Append(',');
//                         collectedInsights.Append(json);
//                         firstInsight = false;
//                     }
//                     collectedInsights.Append(']');
//                     pipelineResult = collectedInsights.ToString();
//                     break;

//                 case AgentType.ProductsRecomendationAgent when isLast:
//                     var recommendations = await _productRecomendationAgent.GetProductRecommendationsAsync(ct);
//                     foreach (var rec in recommendations)
//                         yield return JsonSerializer.Serialize(rec, JsonOptions);
//                     break;

//                 // case AgentType.FinancialDocumentsSearchAgent when isLast:
//                 //     var searchResult = await SafeGetSearchAsync(ct);
//                 //     if (!string.IsNullOrEmpty(searchResult))
//                 //         yield return searchResult;
//                 //     break;

//                 default:
//                     pipelineResult = await ExecuteAgentAsync(userId, pipelineResult, conversationHistory, agentType, from, to, ct);
//                     yield return pipelineResult;
//                     break;
//             }
//         }
//     }

//     public Task<string> GetDocumentSearchResultsAsync(CancellationToken ct)
//         => _financialSearchAgent.GetSearchResultsAsync(ct);

//     private async Task<string> ExecuteAgentAsync(
//         string userId,
//         string input,
//         List<ConversationMessage> conversationHistory,
//         AgentType agentType,
//         DateTimeOffset? from,
//         DateTimeOffset? to,
//         CancellationToken ct)
//     {
//         return agentType switch
//         {
//             AgentType.AdvisorAgent =>
//                 await _advisorAgent.GetAdviceAsync(userId, input, conversationHistory, ct),

//             // AgentType.FinancialDocumentsSearchAgent =>
//             //     await _financialSearchAgent.GetSearchResultsAsync(ct),

//             AgentType.InsightsAgent =>
//                 JsonSerializer.Serialize(
//                     await _insightsAgent.GetInsightsAsync(from ?? DefaultFrom(), to ?? DefaultTo(), ct),
//                     JsonOptions),

//             AgentType.ProductsRecomendationAgent =>
//                 JsonSerializer.Serialize(
//                     await _productRecomendationAgent.GetProductRecommendationsAsync(ct),
//                     JsonOptions),

//             _ => throw new ArgumentException($"Unknown agent type: {agentType}", nameof(agentType))
//         };
//     }

//     private async Task<string> SafeGetSearchAsync(CancellationToken ct)
//     {
//         try { return await _financialSearchAgent.GetSearchResultsAsync(ct); }
//         catch (Exception ex)
//         {
//             _logger.LogWarning(ex, "Document search streaming failed");
//             return string.Empty;
//         }
//     }

//     private static DateTimeOffset DefaultTo() => DateTimeOffset.UtcNow;
//     private static DateTimeOffset DefaultFrom() => DateTimeOffset.UtcNow.AddDays(-30);
// }
