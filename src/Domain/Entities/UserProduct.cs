namespace AiAdvisor.Domain.Entities;

public class UserProduct : BaseAuditableEntity
{
    public Guid ProductId { get; set; }

    public string UserId { get; set; } = string.Empty;

    public decimal AvailableBalance { get; set; }

    public bool IsActive { get; set; }

    public string? CardNumber { get; set; }

    public string? AccountNumber { get; set; }

    public Product Product { get; set; } = null!;
}
