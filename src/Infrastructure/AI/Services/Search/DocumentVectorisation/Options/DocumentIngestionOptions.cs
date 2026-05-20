namespace AiAdvisor.Infrastructure.AI.Services.Options;

/// <summary>
/// Configuration options for document ingestion source.
/// Supports both local folder and Azure Blob Storage sources.
/// </summary>
public class DocumentIngestionOptions
{
    /// <summary>
    /// The source type for document ingestion: "Folder" or "Blob"
    /// </summary>
    public string SourceType { get; set; } = "Folder";

    /// <summary>
    /// Local folder path for markdown documents (used when SourceType = "Folder")
    /// </summary>
    public string? DocumentsPath { get; set; }

    /// <summary>
    /// Azure Blob Storage container URI (used when SourceType = "Blob")
    /// Example: https://mystorageaccount.blob.core.windows.net/documents
    /// </summary>
    public string? BlobContainerUri { get; set; }

    /// <summary>
    /// Optional prefix/folder path within the blob container to filter files
    /// Example: "financial-docs/" to only read from that prefix
    /// </summary>
    public string? BlobPrefix { get; set; }
}
