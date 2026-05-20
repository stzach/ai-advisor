namespace AiAdvisor.Infrastructure.AI.Models;

/// <summary>
/// Metadata about a chunk of a financial document, used to track the source
/// and location of indexed content.
/// </summary>
public record DocumentMetadata
{
    /// <summary>
    /// Unique identifier for this chunk (format: {filename}#{index})
    /// </summary>
    public string ChunkId { get; init; } = string.Empty;

    /// <summary>
    /// Original markdown filename (e.g., "BudgetingBasics.md")
    /// </summary>
    public string SourceFileName { get; init; } = string.Empty;

    /// <summary>
    /// Section heading from the document (e.g., "## Budgeting Basics")
    /// </summary>
    public string SectionHeading { get; init; } = string.Empty;

    /// <summary>
    /// Hierarchical heading level (1=##, 2=###, etc.)
    /// </summary>
    public int HeadingLevel { get; init; } = 1;

    /// <summary>
    /// Index/position of this chunk within the document
    /// </summary>
    public int ChunkIndex { get; init; }

    /// <summary>
    /// Total number of chunks in the document
    /// </summary>
    public int TotalChunks { get; init; }

    /// <summary>
    /// Approximate token count for this chunk
    /// </summary>
    public int TokenCount { get; init; }

    /// <summary>
    /// When this chunk was created/indexed
    /// </summary>
    public DateTime IndexedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Original file path (full path to source markdown)
    /// </summary>
    public string SourceFilePath { get; init; } = string.Empty;
}
