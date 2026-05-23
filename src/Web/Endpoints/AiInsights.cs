using System.Text.Json;
using System.Text.Json.Serialization;
using AiAdvisor.Application.AiInsights.Queries.GetAiInsights;
using AiAdvisor.Infrastructure.AI;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AiAdvisor.Web.Endpoints;

public class AiInsights : IEndpointGroup
{
    private static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
    };

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetAiInsights);
        groupBuilder.MapGet("/stream", StreamAiInsights).WithName("StreamAiInsights");
    }

    [EndpointSummary("Get AI-generated financial insights for the current user")]
    [EndpointDescription("Runs the insights AgentPipeline (InsightsAgent) and returns the parsed insights list.")]
    public static async Task<Ok<List<InsightDto>>> GetAiInsights(IAgentsOrchestrator agentsOrchestrator, DateTime from, DateTime to, CancellationToken ct)
    {
        var json = await agentsOrchestrator.ExecuteAgentPipelineAsync(
            userId: string.Empty,
            message: string.Empty,
            conversationHistory: [],
            pipeline: new AgentPipeline(AgentType.InsightsAgent),
            ct: ct,
            from: new DateTimeOffset(from, TimeSpan.Zero),
            to:   new DateTimeOffset(to,   TimeSpan.Zero));

        var insights = JsonSerializer.Deserialize<List<InsightDto>>(json, CamelCase) ?? [];
        return TypedResults.Ok(insights);
    }

    public static async Task StreamAiInsights(
        HttpContext context,
        IAgentsOrchestrator agentsOrchestrator,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        context.Response.Headers["Content-Type"]      = "text/event-stream; charset=utf-8";
        context.Response.Headers["Cache-Control"]     = "no-cache, no-store";
        context.Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var chunk in agentsOrchestrator.StreamAgentPipelineAsync(
            userId: string.Empty,
            message: string.Empty,
            conversationHistory: [],
            pipeline: AgentPipeline.InsightsPipeline,
            ct: ct,
            from: new DateTimeOffset(from, TimeSpan.Zero),
            to:   new DateTimeOffset(to,   TimeSpan.Zero)))
        {
            var insight = JsonSerializer.Deserialize<InsightDto>(chunk, CamelCase);
            if (insight is null) continue;

            var data = JsonSerializer.Serialize(insight, CamelCase);
            await context.Response.WriteAsync($"data: {data}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }

        await context.Response.WriteAsync("event: done\ndata: {}\n\n", ct);
        await context.Response.Body.FlushAsync(ct);
    }
}
