using AiAdvisor.Application.Common.Security;

namespace AiAdvisor.Application.UserTransactions.Queries.GetUserTransactions;

[Authorize]
public record GetUserTransactionsQuery(DateTimeOffset? From = null, DateTimeOffset? To = null) : IRequest<List<UserTransactionDto>>;
