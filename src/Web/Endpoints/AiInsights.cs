using System.Text.Json;
using System.Text.Json.Serialization;
using AiAdvisor.Application.AiInsights.Queries.GetAiInsights;
using AiAdvisor.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AiAdvisor.Web.Endpoints;

public class AiInsights : IEndpointGroup
{
    private static readonly JsonSerializerOptions CamelCase = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
    };

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetAiInsights);
        groupBuilder.MapGet("/stream", StreamAiInsights).WithName("StreamAiInsights");
    }

    [EndpointSummary("Get AI-generated financial insights for the current user")]
    [EndpointDescription("Calls the insights orchestrator which analyses the user's transactions and products via AI agents and returns a list of personalised insights.")]
    public static async Task<Ok<List<InsightDto>>> GetAiInsights(ISender sender, DateTime from, DateTime to)
    {
        var result = await sender.Send(new GetAiInsightsQuery(
            new DateTimeOffset(from, TimeSpan.Zero),
            new DateTimeOffset(to,   TimeSpan.Zero)
        ));
        return TypedResults.Ok(result);
    }

    public static async Task StreamAiInsights(
        HttpContext context,
        IInsightsOrchestrator orchestrator,
        DateTime from,
        DateTime to,
        CancellationToken ct)
    {
        context.Response.Headers["Content-Type"]      = "text/event-stream; charset=utf-8";
        context.Response.Headers["Cache-Control"]     = "no-cache, no-store";
        context.Response.Headers["X-Accel-Buffering"] = "no";

        await foreach (var insight in orchestrator.StreamInsightsAsync(
            new DateTimeOffset(from, TimeSpan.Zero),
            new DateTimeOffset(to,   TimeSpan.Zero),
            ct))
        {
            var data = JsonSerializer.Serialize(insight, CamelCase);
            await context.Response.WriteAsync($"data: {data}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }

        await context.Response.WriteAsync("event: done\ndata: {}\n\n", ct);
        await context.Response.Body.FlushAsync(ct);
    }
}