using AiAdvisor.Application.Common.Interfaces;

namespace AiAdvisor.Application.AiInsights.Queries.GetAiInsights;

public class GetAiInsightsQueryHandler : IRequestHandler<GetAiInsightsQuery, List<InsightDto>>
{
    private readonly IInsightsOrchestrator _orchestrator;

    public GetAiInsightsQueryHandler(IInsightsOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public Task<List<InsightDto>> Handle(GetAiInsightsQuery request, CancellationToken cancellationToken)
        => _orchestrator.GetInsightsAsync(cancellationToken);
}
