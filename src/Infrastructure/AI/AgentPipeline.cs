namespace AiAdvisor.Infrastructure.AI;

/// <summary>
/// Defines the types of agents available in the pipeline
/// </summary>
public enum AgentType
{
    AdvisorAgent,
    // FinancialDocumentsSearchAgent,
    InsightsAgent,
    ProductsRecomendationAgent
}

/// <summary>
/// Represents a pipeline configuration that defines which agents to execute in sequence
/// </summary>
public class AgentPipeline
{
    public List<AgentType> Agents { get; init; } = [];

    public AgentPipeline(params AgentType[] agents)
    {
        Agents = agents.ToList();
    }

    public static readonly AgentPipeline ChatPipeline = new(
        AgentType.ProductsRecomendationAgent,
        AgentType.AdvisorAgent
    );

    public static readonly AgentPipeline InsightsPipeline = new(
        AgentType.ProductsRecomendationAgent,
        AgentType.InsightsAgent
    );

    public static readonly AgentPipeline AdvisorOnlyPipeline = new(
        AgentType.AdvisorAgent
    );
}
