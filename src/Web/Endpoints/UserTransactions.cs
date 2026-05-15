using AiAdvisor.Application.UserTransactions.Queries.GetUserTransactions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AiAdvisor.Web.Endpoints;

public class UserTransactions : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetUserTransactions);
    }

    [EndpointSummary("Get transactions for the current user")]
    [EndpointDescription("Returns all transactions for the currently authenticated user, ordered by most recent first.")]
    public static async Task<Ok<List<UserTransactionDto>>> GetUserTransactions(ISender sender)
    {
        var result = await sender.Send(new GetUserTransactionsQuery());
        return TypedResults.Ok(result);
    }
}
