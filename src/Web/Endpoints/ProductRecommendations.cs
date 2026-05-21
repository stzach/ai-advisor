using AiAdvisor.Application;
using AiAdvisor.Application.Common.Interfaces;
using AiAdvisor.Application.ProductRecommendations.Queries.GetProductRecommendations;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AiAdvisor.Web.Endpoints;

public class ProductRecommendations : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();
        groupBuilder.MapGet(GetProductRecommendations);
    }

    [EndpointSummary("Get AI-generated product recommendations for the current user")]
    [EndpointDescription("Calls the product recommendation agent and returns a list of recommended products with reasons and redirect URLs.")]
    public static async Task<Ok<List<ProductRecomendationDto>>> GetProductRecommendations(ISender sender)
    {
        var result = await sender.Send(new GetProductRecommendationsQuery());
        return TypedResults.Ok(result);
    }
}
