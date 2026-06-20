using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Graduation_Project.Hubs
{
    // Real-time medication tracker updates.
    // Messages are pushed from the controller via IHubContext, scoped to the
    // owning patient with Clients.User(userId) — no client-invoked methods needed.
    [Authorize]
    public class MedicationHub : Hub
    {
    }
}
