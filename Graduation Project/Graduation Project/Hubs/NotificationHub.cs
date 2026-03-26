using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Graduation_Project.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}
