using AiAdvisor.Application.Common.Security;

namespace AiAdvisor.Application.UserTransactions.Queries.GetUserTransactions;

[Authorize]
public record GetUserTransactionsQuery : IRequest<List<UserTransactionDto>>;
