using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiAdvisor.Infrastructure.AI.Services;

public class DocumentVectorizationBackgroundService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DocumentVectorizationBackgroundService> _logger;

    public DocumentVectorizationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DocumentVectorizationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting vectorization...");

        try
        {
            using var scope = _scopeFactory.CreateScope();

            var vectorizationService =
                scope.ServiceProvider.GetRequiredService<IDocumentVectorizationService>();

            var searchService =
                scope.ServiceProvider.GetRequiredService<IAzureSearchService>();

            var result = await vectorizationService
                .VectorizeDocumentsAsync(cancellationToken);

            if (result.Success && result.Chunks.Any())
            {
                await searchService.IndexDocumentsAsync(
                    result.Chunks,
                    cancellationToken);

                _logger.LogInformation(
                    "Vectorization done. Chunks: {Chunks}",
                    result.Chunks.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vectorization failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}