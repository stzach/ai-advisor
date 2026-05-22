using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using AiAdvisor.Infrastructure.AI.Models;
using AiAdvisor.Infrastructure.AI.Services.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure.Storage.Blobs;
using Azure.Identity;
using System.Text;

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
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly ILogger<DocumentVectorizationService> _logger;
    private readonly DocumentIngestionOptions _ingestionOptions;
    private readonly string _documentsDirectory;

    public DocumentVectorizationService(
        SearchClient searchClient,
        IEmbeddingsProvider embeddingsProvider,
        IMarkdownChunkingService chunkingService,
        IPdfTextExtractor pdfExtractor,
        IOptions<DocumentIngestionOptions> ingestionOptions,
        ILogger<DocumentVectorizationService> logger)
    {
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
        _embeddingsProvider = embeddingsProvider ?? throw new ArgumentNullException(nameof(embeddingsProvider));
        _chunkingService = chunkingService ?? throw new ArgumentNullException(nameof(chunkingService));
        _pdfExtractor = pdfExtractor ?? throw new ArgumentNullException(nameof(pdfExtractor));
        _ingestionOptions = ingestionOptions?.Value ?? throw new ArgumentNullException(nameof(ingestionOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // For folder source, construct documents directory relative to the application
        // This is kept for backward compatibility and folder mode
        var libraryPath = Path.GetDirectoryName(typeof(DocumentVectorizationService).Assembly.Location)!;
        _documentsDirectory = Path.Combine(
            libraryPath,
            "AI",
            "FinancialDocuments");
        // _documentsDirectory = Path.Combine(
        //     AppContext.BaseDirectory,
        //     "..",
        //     "..",
        //     "..",
        //     "Infrastructure",
        //     "AI",
        //     "FinancialDocuments");

        // Normalize path
        _documentsDirectory = Path.GetFullPath(_documentsDirectory);
    }

    public string GetDocumentsDirectory() => _ingestionOptions.SourceType == "Blob"
        ? _ingestionOptions.BlobContainerUri ?? "blob://unknown"
        : _ingestionOptions.DocumentsPath ?? _documentsDirectory;

    public async Task<VectorizationResult> VectorizeDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            return _ingestionOptions.SourceType == "Blob"
                ? await VectorizeDocumentsFromBlobAsync(startTime, cancellationToken)
                : await VectorizeDocumentsFromFolderAsync(startTime, cancellationToken);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Vectorization failed: {ex.Message}";
            _logger.LogError(ex, errorMsg);
            return new VectorizationResult { ErrorMessage = errorMsg };
        }
    }

    private async Task<VectorizationResult> VectorizeDocumentsFromFolderAsync(DateTime startTime, CancellationToken cancellationToken)
    {
        var documentsPath = _ingestionOptions.DocumentsPath ?? _documentsDirectory;

        _logger.LogInformation("Starting document vectorization from folder. Documents directory: {Directory}", documentsPath);

        // Ensure documents directory exists
        if (!Directory.Exists(documentsPath))
        {
            Directory.CreateDirectory(documentsPath);
            _logger.LogWarning("Documents directory created (was empty): {Directory}", documentsPath);
            return new VectorizationResult { DocumentsProcessed = 0, ChunksIndexed = 0 };
        }

        var mdFiles = Directory.GetFiles(documentsPath, "*.md", SearchOption.AllDirectories);
        var pdfFiles = Directory.GetFiles(documentsPath, "*.pdf", SearchOption.AllDirectories);

        var allFiles = mdFiles.Concat(pdfFiles).ToArray();
        _logger.LogInformation("Found {MdCount} markdown and {PdfCount} pdf documents in folder", mdFiles.Length, pdfFiles.Length);

        if (allFiles.Length == 0)
        {
            _logger.LogWarning("No documents found in {Directory}", documentsPath);
            return new VectorizationResult { DocumentsProcessed = 0, ChunksIndexed = 0 };
        }

        return await ProcessFilesAsync(allFiles, startTime, cancellationToken);
    }

    private async Task<VectorizationResult> VectorizeDocumentsFromBlobAsync(DateTime startTime, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_ingestionOptions.BlobContainerUri))
        {
            throw new InvalidOperationException("BlobContainerUri must be configured when SourceType is 'Blob'");
        }

        _logger.LogInformation("Starting document vectorization from blob storage. Container URI: {ContainerUri}, Prefix: {Prefix}",
            _ingestionOptions.BlobContainerUri,
            _ingestionOptions.BlobPrefix ?? "(none)");

        var containerUri = new Uri(_ingestionOptions.BlobContainerUri);
        var containerClient = new BlobContainerClient(containerUri, new DefaultAzureCredential());

        // List all markdown and pdf blobs
        var blobs = new List<(string Name, BlobClient Client)>();
        var prefix = _ingestionOptions.BlobPrefix ?? string.Empty;

        await foreach (var blobItem in containerClient.GetBlobsAsync(
            Azure.Storage.Blobs.Models.BlobTraits.None,
            Azure.Storage.Blobs.Models.BlobStates.None,
            prefix,
            cancellationToken))
        {
            if (blobItem.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                blobItem.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var blobClient = containerClient.GetBlobClient(blobItem.Name);
                blobs.Add((blobItem.Name, blobClient));
            }
        }
        _logger.LogInformation("Found {Count} documents in blob storage", blobs.Count);

        if (blobs.Count == 0)
        {
            _logger.LogWarning("No documents found in blob container: {ContainerUri}", _ingestionOptions.BlobContainerUri);
            return new VectorizationResult { DocumentsProcessed = 0, ChunksIndexed = 0 };
        }

        // Process each blob
        var allChunks = new List<SearchDocumentChunk>();
        var totalTokens = 0L;

        foreach (var (blobName, blobClient) in blobs)
        {
            try
            {
                _logger.LogInformation("Processing blob: {BlobName}", blobName);

                IReadOnlyList<(string Content, DocumentMetadata Metadata)> chunks;

                if (blobName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    // For PDFs, stream to extractor
                    var download = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
                    using var stream = download;
                    var text = await _pdfExtractor.ExtractTextAsync(stream);

                    chunks = await _chunkingService.ChunkContentAsync(
                        text,
                        Path.GetFileName(blobName),
                        blobName,
                        maxTokensPerChunk: 500,
                        overlapTokens: 50);
                }
                else
                {
                    // Assume markdown/plain text
                    var download = await blobClient.DownloadAsync(cancellationToken: cancellationToken);
                    using var reader = new StreamReader(download.Value.Content, Encoding.UTF8);
                    var content = await reader.ReadToEndAsync();

                    // Chunk the content
                    chunks = await _chunkingService.ChunkContentAsync(
                        content,
                        Path.GetFileName(blobName),
                        blobName,
                        maxTokensPerChunk: 500,
                        overlapTokens: 50);
                }

                _logger.LogInformation("Blob {BlobName} chunked into {ChunkCount} chunks", blobName, chunks.Count);

                // Generate embeddings for each chunk
                foreach (var chunk in chunks)
                {
                    var chunkContent = chunk.Content;
                    var metadata = chunk.Metadata;

                    var embedding = await GenerateEmbeddingAsync(chunkContent, cancellationToken);
                    totalTokens += metadata.TokenCount;

                    var searchDoc = new SearchDocumentChunk
                    {
                        Id = metadata.ChunkId,
                        Content = chunkContent,
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
                _logger.LogError(ex, "Error processing blob: {BlobName}", blobName);
                // Continue with next blob
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
            "Vectorization complete. Chunks: {ChunkCount}, Tokens: {Tokens}, Duration: {Duration}",
            allChunks.Count,
            totalTokens,
            duration);

        return new VectorizationResult
        {
            DocumentsProcessed = blobs.Count,
            ChunksIndexed = allChunks.Count,
            TotalTokensEmbedded = totalTokens,
            Duration = duration
        };
    }

    private async Task<VectorizationResult> ProcessFilesAsync(
        string[] files,
        DateTime startTime,
        CancellationToken cancellationToken)
    {
        // Process each document
        var allChunks = new List<SearchDocumentChunk>();
        var totalTokens = 0L;

        foreach (var filePath in files)
        {
            try
            {
                _logger.LogInformation("Processing document: {FileName}", Path.GetFileName(filePath));

                IReadOnlyList<(string Content, DocumentMetadata Metadata)> chunks;

                if (filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    await using var fs = File.OpenRead(filePath);
                    var text = await _pdfExtractor.ExtractTextAsync(fs);
                    chunks = await _chunkingService.ChunkContentAsync(text, Path.GetFileName(filePath), filePath);
                }
                else
                {
                    // markdown or plain text
                    chunks = await _chunkingService.ChunkDocumentAsync(filePath);
                }

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
            files.Length,
            allChunks.Count,
            totalTokens,
            duration);

        return new VectorizationResult
        {
            DocumentsProcessed = files.Length,
            ChunksIndexed = allChunks.Count,
            TotalTokensEmbedded = totalTokens,
            Duration = duration
        };
    }

    private async Task<VectorizationResult> ProcessMarkdownFilesAsync(
        string[] markdownFiles,
        DateTime startTime,
        CancellationToken cancellationToken)
    {
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

    private async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        // Use embeddings provider to generate embedding
        var embedding = await _embeddingsProvider.GenerateEmbeddingAsync(text);

        // Convert to List<float> for Azure Search
        return embedding.ToArray();
    }
}
