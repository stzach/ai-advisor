using AiAdvisor.Domain.Enums;

namespace AiAdvisor.Domain.Entities;

public class UserTransaction : BaseAuditableEntity
{
    public Guid TransactionId { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    public Guid ProductId { get; set; }

    public string? From { get; set; }

    public string? To { get; set; }

    public TransactionType TransactionType { get; set; }

    public TransactionCategory TransactionCategory { get; set; }

    public decimal Amount { get; set; }

    public Product Product { get; set; } = null!;
}