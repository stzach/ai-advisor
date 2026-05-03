using AiAdvisor.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AiAdvisor.Web.Services;

public class NotificationBackgroundService(IHubContext<NotificationHub> hubContext, ILogger<NotificationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await hubContext.Clients.All.SendAsync("ReceiveNotification", $"Server time: {DateTime.UtcNow:HH:mm:ss}", stoppingToken);
                logger.LogInformation("Broadcast sent at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending notification");
            }
        }
    }
}
