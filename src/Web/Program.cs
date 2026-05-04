using AiAdvisor.Infrastructure.Data;
using Scalar.AspNetCore;
using AiAdvisor.Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.AddServiceDefaults();

builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

builder.AddAzureChatCompletionsClient(connectionName: "ai-foundry")
    .AddChatClient("deploymentName");

var signalRBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("signalr")))
    signalRBuilder.AddNamedAzureSignalR("signalr");


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

app.MapHub<NotificationHub>("/chat").ExcludeFromApiReference().ExcludeFromDescription();

app.MapFallbackToFile("index.html");

app.Run();
