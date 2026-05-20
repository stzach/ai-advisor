using AiAdvisor.Application.Common.Security;

namespace AiAdvisor.Application.AiInsights.Queries.GetAiInsights;

[Authorize]
public record GetAiInsightsQuery : IRequest<List<InsightDto>>;
