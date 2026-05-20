using AiAdvisor.Application.Common.Security;

namespace AiAdvisor.Application.AiInsights.Queries.GetAiInsights;

[Authorize]
public record GetAiInsightsQuery(DateTimeOffset From, DateTimeOffset To) : IRequest<List<InsightDto>>;
