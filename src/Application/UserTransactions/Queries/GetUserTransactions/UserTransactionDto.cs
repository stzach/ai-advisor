namespace AiAdvisor.Application.UserTransactions.Queries.GetUserTransactions;

public class UserTransactionDto
{
    public Guid TransactionId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string TransactionType { get; init; } = string.Empty;
    public string TransactionCategory { get; init; } = string.Empty;
    public string TransactionDirection { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
    public DateTimeOffset Created { get; init; }
}