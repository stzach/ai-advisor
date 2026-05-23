using AiAdvisor.Application.Common.Interfaces;

namespace AiAdvisor.Application.AiInsights.Queries.GetAiInsights;

public class GetAiInsightsQueryHandler : IRequestHandler<GetAiInsightsQuery, List<InsightDto>>
{
    private readonly IInsightsAgent _orchestrator;

    public GetAiInsightsQueryHandler(IInsightsAgent orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public Task<List<InsightDto>> Handle(GetAiInsightsQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetInsightsAsync(request.From, request.To, cancellationToken);
}
