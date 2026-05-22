using AiAdvisor.Application.Common.Interfaces;
using AiAdvisor.Infrastructure.AI;
using AiAdvisor.Infrastructure.AI.Services;
using AiAdvisor.Infrastructure.AI.Services.Options;
using AiAdvisor.Infrastructure.Data;
using AiAdvisor.Infrastructure.Data.Interceptors;
using AiAdvisor.Infrastructure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Azure;
using Azure.Identity;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(Services.Database);
        Guard.Against.Null(connectionString, message: $"Connection string '{Services.Database}' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlServer(connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        builder.EnrichSqlServerDbContext<ApplicationDbContext>();

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies();

        builder.Services.AddAuthorizationBuilder();

        builder.Services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders()
            .AddApiEndpoints();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        
        // AI Agents
        builder.Services.AddScoped<IChatService, ChatService>();
        builder.Services.AddScoped<IFinancialDataAgent, FinancialDataAgent>();
        builder.Services.AddScoped<IAdvisorAgent, AdvisorAgent>();

        // Document Vectorization & Search
        builder.Services.AddSingleton<IMarkdownChunkingService, MarkdownChunkingService>();
        builder.Services.AddScoped<IPdfTextExtractor, PdfPigPdfTextExtractor>();
        builder.Services.AddOptions<DocumentIngestionOptions>()
            .Bind(builder.Configuration.GetSection("DocumentIngestion"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddOptions<AzureOpenAiOptions>()
            .Bind(builder.Configuration.GetSection("AzureOpenAIEmbedings"))
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.Endpoint), "Azure OpenAI endpoint must be configured.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Azure OpenAI API key must be configured.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.EmbeddingDeployment), "Azure OpenAI embedding deployment must be configured.")
            .ValidateOnStart();
        builder.Services.AddScoped<IEmbeddingsProvider, EmbeddingsProvider>();
        builder.Services.AddScoped<IDocumentVectorizationService, DocumentVectorizationService>();
        builder.Services.AddScoped<IFinancialDocumentSearchService, FinancialDocumentSearchService>();

        // Background Services
        builder.Services.AddHostedService<DocumentVectorizationBackgroundService>();

        // Azure Search clients
        builder.AddAzureSearchClient(Services.Search);

        
        builder.Services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var endpoint = new Uri(builder.Configuration["AzureSearch:Endpoint"]);
            var apiKey = builder.Configuration["AzureSearch:ApiKey"];
            var indexName = "documents_index";

            return new SearchClient(endpoint, indexName, new AzureKeyCredential(apiKey));
        });

        builder.Services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var endpoint = new Uri(builder.Configuration["AzureSearch:Endpoint"]);
            var apiKey = builder.Configuration["AzureSearch:ApiKey"];

            return new SearchIndexClient(endpoint, new AzureKeyCredential(apiKey));
        });

        builder.Services.AddScoped<IInsightsOrchestrator, InsightsOrchestrator>();

        builder.Services.AddScoped<IFinancialDocumentsSearchAgent, FinancialDocumentsSearchAgent>();
        builder.Services.AddScoped<IProductRecomendationAgent, ProductRecomendationAgent>();
    }
}
