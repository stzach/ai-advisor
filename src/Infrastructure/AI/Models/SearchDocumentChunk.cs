using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;

namespace AiAdvisor.Infrastructure.AI.Models;

/// <summary>
/// Document model that maps to Azure AI Search index for financial documents.
/// </summary>
public class SearchDocumentChunk
{
    /// <summary>
    /// Unique identifier for this document chunk in the search index
    /// </summary>
    [SimpleField(IsKey = true)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The actual text content of this document chunk
    /// </summary>
    [SearchableField(IsFilterable = true)]
    public string Content { get; set; } = string.Empty;

    [VectorSearchField(
        VectorSearchDimensions = 3072,
        VectorSearchProfileName = "default")]
    public float[]? ContentVector { get; set; }
    /// <summary>
    /// Original source filename
    /// </summary>
    [SimpleField(IsFilterable = true, IsFacetable = true)]
    public string SourceFileName { get; set; } = string.Empty;

    /// <summary>
    /// Section heading from the document
    /// </summary>
    [SearchableField(IsFilterable = true)]
    public string SectionHeading { get; set; } = string.Empty;

    /// <summary>
    /// Heading level (1=##, 2=###, etc.)
    /// </summary>
    [SimpleField(IsFilterable = true)]
    public int HeadingLevel { get; set; }

    /// <summary>
    /// Chunk index within the document
    /// </summary>
    [SimpleField(IsFilterable = true)]
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Total chunks in the document
    /// </summary>
    [SimpleField(IsFilterable = true)]
    public int TotalChunks { get; set; }

    /// <summary>
    /// Token count for this chunk (for cost tracking)
    /// </summary>
    [SimpleField(IsFilterable = true)]
    public int TokenCount { get; set; }

    /// <summary>
    /// When this document was indexed
    /// </summary>
    [SimpleField(IsFilterable = true, IsSortable = true)]
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
}
