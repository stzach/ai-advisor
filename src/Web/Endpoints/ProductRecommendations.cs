using AiAdvisor.Application;
using AiAdvisor.Application.Common.Interfaces;
using AiAdvisor.Application.ProductRecommendations.Queries.GetProductRecommendations;
using Azure;
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
    public static async Task<Results<Ok<List<ProductRecomendationDto>>, StatusCodeHttpResult>> GetProductRecommendations(ISender sender)
    {
        try
        {
            var result = await sender.Send(new GetProductRecommendationsQuery());
            return TypedResults.Ok(result);
        }
        catch (RequestFailedException ex) when (ex.Status == 429)
        {
            return TypedResults.StatusCode(StatusCodes.Status429TooManyRequests);
        }
    }
}
