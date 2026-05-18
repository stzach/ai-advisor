using AiAdvisor.Shared;
using Aspire.Hosting.Foundry;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca-env");

var databaseServer = builder
    .AddAzureSqlServer(Services.DatabaseServer)
    .RunAsContainer(container =>
        container
            .WithLifetime(ContainerLifetime.Persistent)
            .WithDataVolume())
    .AddDatabase(Services.Database);

var signalR = builder.AddAzureSignalR(Services.SignalR);

var search = builder.AddAzureSearch(Services.Search);

var foundry = builder.AddFoundry(Services.Foundry);

var chat = foundry.AddDeployment(Services.Chat, FoundryModel.Microsoft.Phi4);

var web = builder.AddProject<Projects.Web>(Services.WebApi)
    .WithReference(databaseServer)
    .WaitFor(databaseServer)
    .WithReference(signalR)
    .WaitFor(signalR)
    .WithReference(search)
    .WithReference(chat)
    .WaitFor(chat)
    .WithExternalHttpEndpoints()
    .WithAspNetCoreEnvironment()
    .WithUrlForEndpoint("http", url =>
    {
        url.DisplayText = "Scalar API Reference";
        url.Url = "/scalar";
    });

if (builder.ExecutionContext.IsRunMode)
{
    builder.AddJavaScriptApp(Services.WebFrontend, "./../Web/ClientApp")
        .WithRunScript("start")
        .WithReference(web)
        .WaitFor(web)
        .WithHttpEndpoint(env: "PORT")
        .WithExternalHttpEndpoints();
}


builder.Build().Run();
