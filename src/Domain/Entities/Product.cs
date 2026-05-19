namespace AiAdvisor.Domain.Entities;

public class Product : BaseAuditableEntity
{
    public Guid ProductId { get; set; } = Guid.NewGuid();

    public int AutoId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string? ProductDescription { get; set; }

    public decimal? ProductPrice { get; set; }

    public decimal? CreditLimit { get; set; }

    public ProductType ProductType { get; set; }
}
