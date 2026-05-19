using AiAdvisor.Application.UserTransactions.Commands.CreateUserTransaction;
using AiAdvisor.Application.UserTransactions.Queries.GetUserTransactions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AiAdvisor.Web.Endpoints;

public class UserTransactions : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapGet(GetUserTransactions);
        groupBuilder.MapPost(CreateUserTransaction);
    }

    [EndpointSummary("Get transactions for the current user")]
    [EndpointDescription("Returns all transactions for the currently authenticated user, ordered by most recent first.")]
    public static async Task<Ok<List<UserTransactionDto>>> GetUserTransactions(
        ISender sender,
        DateTimeOffset? from = null,
        DateTimeOffset? to   = null)
    {
        var result = await sender.Send(new GetUserTransactionsQuery { From = from, To = to });
        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create a new transaction for the current user")]
    [EndpointDescription("Creates a new transaction for the currently authenticated user using the specified transaction type, category, direction, and amount.")]
    public static async Task<Created<Guid>> CreateUserTransaction(ISender sender, CreateUserTransactionCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/api/UserTransactions/{id}", id);
    }
}
