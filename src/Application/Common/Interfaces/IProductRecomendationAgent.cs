namespace AiAdvisor.Application.Common.Interfaces;

using AiAdvisor.Application;

public interface IProductRecomendationAgent
{
    Task<List<ProductRecomendationDto>> GetProductRecommendationsAsync(CancellationToken ct);
}