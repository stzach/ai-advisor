using AiAdvisor.Application.AiInsights.Queries.GetAiInsights;

namespace AiAdvisor.Application.Common.Interfaces;

public interface IInsightsAgent
{
    Task<List<InsightDto>> GetInsightsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
    IAsyncEnumerable<InsightDto> StreamInsightsAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default);
}
