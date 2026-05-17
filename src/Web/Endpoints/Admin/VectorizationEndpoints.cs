using AiAdvisor.Infrastructure.AI.Services;
using Microsoft.AspNetCore.Authorization;

namespace AiAdvisor.Web.Endpoints.Admin;

/// <summary>
/// Admin endpoints for managing document vectorization.
/// </summary>
public static class VectorizationEndpoints
{
    public static void MapVectorizationEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("api/admin/vectorization")
            .WithName("Vectorization")
            .WithOpenApi()
            .RequireAuthorization(); // Requires authentication

        group.MapPost("refresh", RefreshDocumentsHandler)
            .WithName("RefreshFinancialDocuments")
            .WithDescription("Re-vectorizes all financial documents and updates the search index. This should be called after updating financial document files.");
       
    }

    /// <summary>
    /// Refreshes (re-vectorizes) all financial documents.
    /// </summary>
    private static async Task<RefreshDocumentsResponse> RefreshDocumentsHandler(
        IDocumentVectorizationService vectorizationService,
        CancellationToken cancellationToken)
    {
        var result = await vectorizationService.VectorizeDocumentsAsync(cancellationToken);

        return new RefreshDocumentsResponse
        {
            Success = result.Success,
            DocumentsProcessed = result.DocumentsProcessed,
            ChunksIndexed = result.ChunksIndexed,
            TotalTokensEmbedded = result.TotalTokensEmbedded,
            DurationSeconds = result.Duration.TotalSeconds,
            Message = result.ErrorMessage ?? "Vectorization completed successfully"
        };
    }
}

/// <summary>
/// Response model for the refresh documents endpoint.
/// </summary>
public record RefreshDocumentsResponse
{
    /// <summary>
    /// Whether the vectorization was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Number of markdown documents processed
    /// </summary>
    public int DocumentsProcessed { get; init; }

    /// <summary>
    /// Number of document chunks indexed in Azure Search
    /// </summary>
    public int ChunksIndexed { get; init; }

    /// <summary>
    /// Total tokens used for embeddings
    /// </summary>
    public long TotalTokensEmbedded { get; init; }

    /// <summary>
    /// Time taken for vectorization (in seconds)
    /// </summary>
    public double DurationSeconds { get; init; }

    /// <summary>
    /// Status message
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
