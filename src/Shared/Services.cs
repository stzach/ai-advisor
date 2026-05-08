namespace AiAdvisor.Shared;

public static class Services
{
    /// <summary>
    /// The name of the Web Frontend service.
    /// This service is responsible for hosting the frontend application.
    /// </summary>
    public const string WebFrontend = "webfrontend";

    /// <summary>
    /// The name of the Web API service.
    /// This service is responsible for hosting the Web API application.
    /// </summary>
    public const string WebApi = "webapi";

    /// <summary>
    /// The name of the Database Server service.
    /// This service is responsible for hosting the database server (e.g., PostgreSQL, SQL Server, or SQLite).
    /// </summary>
    public const string DatabaseServer = "dbserver";

    /// <summary>
    /// The name of the Database.
    /// This is the name of the database that will be created and used by the application.
    /// </summary>
    public const string Database = "AiAdvisorDb";

    /// <summary>
    /// The name of the SignalR service.
    /// </summary>
    public const string SignalR = "signalr";

    /// <summary>
    /// The name of the Chat service.
    /// </summary>
    public const string Chat = "chat";

    /// <summary>
    /// The name of the Foundry service.
    /// </summary>
    public const string Foundry = "foundry";

    /// <summary>
    /// The name of the Azure AI Search service.
    /// </summary>
    public const string Search = "search";
}
