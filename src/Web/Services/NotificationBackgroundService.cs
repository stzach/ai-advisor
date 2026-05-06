using AiAdvisor.Infrastructure.AI;
using AiAdvisor.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AiAdvisor.Web.Services;

public class NotificationBackgroundService(IHubContext<NotificationHub> hubContext, IServiceScopeFactory scopeFactory, ILogger<NotificationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(60));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
                var aiResponse = await chatService.SendAsync("What's the current server time?", stoppingToken);
                await hubContext.Clients.All.SendAsync("ReceiveAINotification", $"AI response: {aiResponse}", stoppingToken);
                logger.LogInformation("AI response: {Response}", aiResponse);
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
