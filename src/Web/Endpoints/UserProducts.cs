using AiAdvisor.Application.UserProducts.Queries.GetUserProducts;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AiAdvisor.Web.Endpoints;

public class UserProducts : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetUserProducts);
    }

    [EndpointSummary("Get products for the current user")]
    [EndpointDescription("Returns all products assigned to the currently authenticated user, including balances and card/account numbers.")]
    public static async Task<Ok<List<UserProductDto>>> GetUserProducts(ISender sender)
    {
        var result = await sender.Send(new GetUserProductsQuery());
        return TypedResults.Ok(result);
    }
}