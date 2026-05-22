using AiAdvisor.Application.Common.Security;

namespace AiAdvisor.Application.ProductRecommendations.Queries.GetProductRecommendations;

[Authorize]
public record GetProductRecommendationsQuery : IRequest<List<ProductRecomendationDto>>;
