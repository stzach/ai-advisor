using System.Linq;
using AiAdvisor.Application.Common.DTOs;
using AiAdvisor.Infrastructure.AI.Services;
using Microsoft.AspNetCore.Authorization;

namespace AiAdvisor.Web.Endpoints;

/// <summary>
/// Endpoints for searching indexed financial documents.
/// </summary>
public static class DocumentSearchEndpoints
{
    public static void MapFinancialDocumentSearchEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("api/financial-documents")
            .WithName("FinancialDocuments")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("search", SearchDocumentsHandler)
            .WithName("SearchDocuments")
            .WithDescription("Search financial documents using vector embeddings.");
    }

    private static async Task<IReadOnlyList<FinancialDocumentSearchResultDto>> SearchDocumentsHandler(
        IFinancialDocumentSearchService searchService,
        DocumentSearchRequest request,
        CancellationToken cancellationToken)
    {
        var topK = request.TopK > 0 ? request.TopK : 5; // Default to 5 if not specified or invalid
        var searchResults = await searchService.SearchDocumentsAsync(request.Query, topK, cancellationToken);

        return searchResults.Select(result => new FinancialDocumentSearchResultDto
        {
            Id = result.Id,
            Content = result.Content,
            SourceFileName = result.SourceFileName,
            SectionHeading = result.SectionHeading,
            RelevanceScore = result.RelevanceScore,
            ChunkIndex = result.ChunkIndex,
            TotalChunks = result.TotalChunks
        }).ToList();
    }
}

/// <summary>
/// Request model for document search.
/// </summary>
public class DocumentSearchRequest
{
    /// <summary>
    /// Search query text.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Number of results to return (default: 5).
    /// </summary>
    public int TopK { get; set; } = 5;
}
