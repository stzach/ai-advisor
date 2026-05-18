using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using AiAdvisor.Infrastructure.AI.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.AI.Services;

/// <summary>
/// Service for vectorizing financial documents and indexing them in Azure AI Search.
/// </summary>
public interface IDocumentVectorizationService
{
    /// <summary>
    /// Vectorizes all markdown documents in the configured documents folder
    /// and indexes them in Azure AI Search. Creates index if it doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with count of documents indexed</returns>
    Task<VectorizationResult> VectorizeDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configured documents directory path.
    /// </summary>
    string GetDocumentsDirectory();
}

/// <summary>
/// Result of vectorization operation.
/// </summary>
public record VectorizationResult
{
    public int DocumentsProcessed { get; init; }
    public int ChunksIndexed { get; init; }
    public long TotalTokensEmbedded { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
    public bool Success => string.IsNullOrEmpty(ErrorMessage);
    public List<SearchDocumentChunk> Chunks { get; set; } = [];

}

/// <summary>
/// Implementation of document vectorization service.
/// </summary>
public class DocumentVectorizationService : IDocumentVectorizationService
{
    private readonly SearchClient _searchClient;
    private readonly IEmbeddingsProvider _embeddingsProvider;
    private readonly IMarkdownChunkingService _chunkingService;
    private readonly ILogger<DocumentVectorizationService> _logger;
    private readonly string _documentsDirectory;

    public DocumentVectorizationService(
        SearchClient searchClient,
        IEmbeddingsProvider embeddingsProvider,
        IMarkdownChunkingService chunkingService,
        ILogger<DocumentVectorizationService> logger)
    {
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        _embeddingsProvider = embeddingsProvider ?? throw new ArgumentNullException(nameof(embeddingsProvider));
        _chunkingService = chunkingService ?? throw new ArgumentNullException(nameof(chunkingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Construct documents directory relative to the application
        _documentsDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "Infrastructure",
            "AI",
            "FinancialDocuments");

        // Normalize path
        _documentsDirectory = Path.GetFullPath(_documentsDirectory);
    }

    public string GetDocumentsDirectory() => _documentsDirectory;

    public async Task<VectorizationResult> VectorizeDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation("Starting document vectorization. Documents directory: {Directory}", _documentsDirectory);

            // Ensure documents directory exists
            if (!Directory.Exists(_documentsDirectory))
            {
                Directory.CreateDirectory(_documentsDirectory);
                _logger.LogWarning("Documents directory created (was empty): {Directory}", _documentsDirectory);
                return new VectorizationResult { DocumentsProcessed = 0, ChunksIndexed = 0 };
            }

            // Get all markdown files
            var markdownFiles = Directory.GetFiles(_documentsDirectory, "*.md", SearchOption.TopDirectoryOnly);
            _logger.LogInformation("Found {Count} markdown documents", markdownFiles.Length);

            if (markdownFiles.Length == 0)
            {
                _logger.LogWarning("No markdown documents found in {Directory}", _documentsDirectory);
                return new VectorizationResult { DocumentsProcessed = 0, ChunksIndexed = 0 };
            }

            // Process each document
            var allChunks = new List<SearchDocumentChunk>();
            var totalTokens = 0L;

            foreach (var filePath in markdownFiles)
            {
                try
                {
                    _logger.LogInformation("Processing document: {FileName}", Path.GetFileName(filePath));

                    // Chunk the document
                    var chunks = await _chunkingService.ChunkDocumentAsync(filePath);
                    _logger.LogInformation("Document chunked into {ChunkCount} chunks", chunks.Count);

                    // Generate embeddings for each chunk
                   foreach (var chunk in chunks)
                    {
                        var content = chunk.Content;
                        var metadata = chunk.Metadata;

                        var embedding = await GenerateEmbeddingAsync(content, cancellationToken);
                        totalTokens += metadata.TokenCount;

                        var searchDoc = new SearchDocumentChunk
                        {
                            Id = metadata.ChunkId,
                            Content = content,
                            ContentVector = embedding,
                            SourceFileName = metadata.SourceFileName,
                            SectionHeading = metadata.SectionHeading,
                            HeadingLevel = metadata.HeadingLevel,
                            ChunkIndex = metadata.ChunkIndex,
                            TotalChunks = metadata.TotalChunks,
                            TokenCount = metadata.TokenCount,
                            IndexedAt = DateTime.UtcNow
                        };

                        allChunks.Add(searchDoc);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing document: {FileName}", Path.GetFileName(filePath));
                    // Continue with next document
                }
            }

            // Index all chunks
            if (allChunks.Count > 0)
            {
                _logger.LogInformation("Uploading {ChunkCount} chunks to search index", allChunks.Count);
                var result = await _searchClient.MergeOrUploadDocumentsAsync(allChunks, cancellationToken: cancellationToken);
                _logger.LogInformation("Successfully indexed {SuccessCount} documents", result.Value.Results.Count(r => r.Succeeded));
            }

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Vectorization complete. Documents: {DocCount}, Chunks: {ChunkCount}, Tokens: {Tokens}, Duration: {Duration}",
                markdownFiles.Length,
                allChunks.Count,
                totalTokens,
                duration);

            return new VectorizationResult
            {
                DocumentsProcessed = markdownFiles.Length,
                ChunksIndexed = allChunks.Count,
                TotalTokensEmbedded = totalTokens,
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            var errorMsg = $"Vectorization failed: {ex.Message}";
            _logger.LogError(ex, errorMsg);
            return new VectorizationResult { ErrorMessage = errorMsg };
        }
    }

    private async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        // Use embeddings provider to generate embedding
        var embedding = await _embeddingsProvider.GenerateEmbeddingAsync(text);

        // Convert to List<float> for Azure Search
        return embedding.ToArray();
    }
}
