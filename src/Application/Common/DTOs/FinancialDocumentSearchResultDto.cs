namespace AiAdvisor.Application.Common.DTOs;

/// <summary>
/// DTO for returning financial document search results to the client.
/// </summary>
public record FinancialDocumentSearchResultDto
{
    /// <summary>
    /// Unique identifier for this document chunk
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// The text content of this document chunk
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Source filename (e.g., "BudgetingBasics.md")
    /// </summary>
    public string SourceFileName { get; init; } = string.Empty;

    /// <summary>
    /// Section heading from the document
    /// </summary>
    public string SectionHeading { get; init; } = string.Empty;

    /// <summary>
    /// Relevance score (0-1, where 1 is most relevant)
    /// </summary>
    public double RelevanceScore { get; init; }

    /// <summary>
    /// Index of this chunk within the document
    /// </summary>
    public int ChunkIndex { get; init; }

    /// <summary>
    /// Total number of chunks in the document
    /// </summary>
    public int TotalChunks { get; init; }
}
