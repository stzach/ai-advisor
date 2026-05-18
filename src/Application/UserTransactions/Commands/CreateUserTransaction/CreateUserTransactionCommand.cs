using AiAdvisor.Application.Common.Security;
using AiAdvisor.Domain.Enums;

namespace AiAdvisor.Application.UserTransactions.Commands.CreateUserTransaction;

[Authorize]
public record CreateUserTransactionCommand : IRequest<Guid>
{
    public Guid ProductId { get; init; }
    public TransactionType TransactionType { get; init; }
    public TransactionCategory TransactionCategory { get; init; }
    public TransactionDirection TransactionDirection { get; init; }
    public decimal Amount { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
}
