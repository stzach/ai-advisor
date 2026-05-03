using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AiAdvisor.Web.Hubs;

[Authorize]
public class NotificationHub(ILogger<NotificationHub> logger) : Hub
{
    public async Task Send(string name, string message)
    {
        // Call the broadcastMessage method to update clients.
        await Clients.All.SendAsync("broadcastMessage", name, message);
    }

    public override Task OnConnectedAsync()
    {
        logger.LogInformation("User {UserId} connected with connection {ConnectionId}",
            Context.UserIdentifier, Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation("User {UserId} disconnected with connection {ConnectionId}",
            Context.UserIdentifier, Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
