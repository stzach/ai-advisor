namespace AiAdvisor.Application.UserProducts.Queries.GetUserProducts;

public class UserProductDto
{
    public int Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductDescription { get; init; }
    public string ProductType { get; init; } = string.Empty;
    public decimal AvailableBalance { get; init; }
    public bool IsActive { get; init; }
    public string? CardNumber { get; init; }
    public string? AccountNumber { get; init; }
}