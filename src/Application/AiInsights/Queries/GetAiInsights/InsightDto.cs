namespace AiAdvisor.Application.AiInsights.Queries.GetAiInsights;

public class InsightDto
{
    public string Icon    { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Cta     { get; init; } = string.Empty;
    public string Prompt  { get; init; } = string.Empty;
}
