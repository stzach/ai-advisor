using AiAdvisor.Application.Common.Security;

namespace AiAdvisor.Application.UserTransactions.Queries.GetUserTransactions;

[Authorize]
public record GetUserTransactionsQuery : IRequest<List<UserTransactionDto>>
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To   { get; init; }
}
