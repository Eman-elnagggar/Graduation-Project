using Graduation_Project.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Graduation_Project.Services
{
    public class NotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task SendAlertAsync(string userId, object payload, CancellationToken ct = default)
        {
            try
            {
                await _hubContext.Clients.User(userId).SendAsync("AlertCreated", payload, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send realtime alert to user {UserId}", userId);
            }
        }
    }
}