using AiAdvisor.Infrastructure.Data;
using AiAdvisor.Shared;
using AiAdvisor.Web.Endpoints;
using AiAdvisor.Web.Endpoints.Admin;
using Scalar.AspNetCore;
using AiAdvisor.Web.Hubs;
using AiAdvisor.Infrastructure.AI.Services;
using Microsoft.Extensions.AI;
using AiAdvisor.Infrastructure.AI.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var chatClient = builder.AddAzureChatCompletionsClient(connectionName: Services.Chat)
    .AddChatClient(Services.Chat);   

var searchCs = builder.Configuration.GetConnectionString(Services.Search);
if (!string.IsNullOrEmpty(searchCs))
{
    builder.AddAzureSearchClient(connectionName: Services.Search);
    builder.Services.AddSingleton<IAzureSearchService, AzureSearchService>();
    builder.Services.AddSingleton<DocumentVectorizationBackgroundService>();
}

var signalRBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString(Services.SignalR)))
    signalRBuilder.AddNamedAzureSignalR(Services.SignalR);

var app = builder.Build();


// Configure the HTTP request pipeline.
await app.InitialiseDatabaseAsync();

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors(static builder =>
    builder.AllowAnyMethod()
        .AllowAnyHeader()
        .AllowAnyOrigin());

app.UseFileServer();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseExceptionHandler(options => { });

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapEndpoints(typeof(Program).Assembly);
app.MapVectorizationEndpoints();
app.MapFinancialDocumentSearchEndpoints();

var azureSearchEndpoint = builder.Configuration["AzureSearch:Endpoint"];
var azureSearchApiKey   = builder.Configuration["AzureSearch:ApiKey"];
if (!string.IsNullOrEmpty(azureSearchEndpoint) && !string.IsNullOrEmpty(azureSearchApiKey))
{
    var creator = new AzureSearchIndexCreator(endpoint: azureSearchEndpoint, apiKey: azureSearchApiKey);
    await creator.CreateIndexAsync("documents_index");
}

app.MapHub<NotificationHub>("/chat").ExcludeFromApiReference().ExcludeFromDescription();
app.MapHub<ChatHub>("/ai-chat").ExcludeFromApiReference().ExcludeFromDescription();


app.MapFallbackToFile("index.html");

app.Run();
