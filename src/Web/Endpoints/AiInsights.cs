using AiAdvisor.Application.AiInsights.Queries.GetAiInsights;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AiAdvisor.Web.Endpoints;

public class AiInsights : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetAiInsights);
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
}
