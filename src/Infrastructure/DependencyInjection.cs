using AiAdvisor.Application.Common.Interfaces;
using AiAdvisor.Infrastructure.AI;
using AiAdvisor.Infrastructure.AI.Services;
using AiAdvisor.Infrastructure.AI.Tools;
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
using AiAdvisor.Infrastructure.AI.Services.Options;

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
        builder.Services.AddScoped<FinancialDocumentSearchTool>();

        // Background Services
        builder.Services.AddHostedService<DocumentVectorizationBackgroundService>();

        // Azure Search clients
        builder.AddAzureSearchClient(Services.Search);

        
        builder.Services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var endpoint = new Uri(config["AzureSearch:Endpoint"]);
            var indexName = config["AzureSearch:IndexName"];
            var apiKey = config["AzureSearch:ApiKey"];

            return new SearchClient(endpoint, indexName, new AzureKeyCredential(apiKey));
        });

        builder.Services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            var endpoint = new Uri(config["AzureSearch:Endpoint"]);
            var indexName = config["AzureSearch:IndexName"];
            var apiKey = config["AzureSearch:ApiKey"];

            return new SearchIndexClient(endpoint, new AzureKeyCredential(apiKey));
        });
    }
}
