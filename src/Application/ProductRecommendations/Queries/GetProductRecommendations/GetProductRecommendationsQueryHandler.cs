using AiAdvisor.Application.Common.Interfaces;

namespace AiAdvisor.Application.ProductRecommendations.Queries.GetProductRecommendations;

public class GetProductRecommendationsQueryHandler : IRequestHandler<GetProductRecommendationsQuery, List<ProductRecomendationDto>>
{
    private readonly IProductRecomendationAgent _productRecomendationAgent;

    public GetProductRecommendationsQueryHandler(IProductRecomendationAgent productRecomendationAgent)
    {
        _productRecomendationAgent = productRecomendationAgent;
    }

    public Task<List<ProductRecomendationDto>> Handle(GetProductRecommendationsQuery request, CancellationToken cancellationToken)
        => _productRecomendationAgent.GetProductRecommendationsAsync(cancellationToken);
}
