using AiAdvisor.Infrastructure.Data;
using AiAdvisor.Shared;
using AiAdvisor.Web.Endpoints.Admin;
using Scalar.AspNetCore;
using AiAdvisor.Web.Hubs;
using AiAdvisor.Infrastructure.AI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

builder.AddAzureChatCompletionsClient(connectionName: Services.Chat)
    .AddChatClient(Services.Chat);

builder.AddAzureSearchClient(connectionName: Services.Search);

var signalRBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString(Services.SignalR)))
    signalRBuilder.AddNamedAzureSignalR(Services.SignalR);

builder.Services.AddSingleton<IAzureSearchService, AzureSearchService>();

builder.Services.AddSingleton<DocumentVectorizationBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
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


var creator = new AzureSearchIndexCreator(
    endpoint: builder.Configuration.GetValue<string>("AzureSearch:Endpoint"),
    apiKey: builder.Configuration.GetValue<string>("AzureSearch:ApiKey"));

await creator.CreateIndexAsync( builder.Configuration.GetValue<string>("AzureSearch:indexName"));

app.MapHub<NotificationHub>("/chat").ExcludeFromApiReference().ExcludeFromDescription();
app.MapHub<ChatHub>("/ai-chat").ExcludeFromApiReference().ExcludeFromDescription();


app.MapFallbackToFile("index.html");

app.Run();
