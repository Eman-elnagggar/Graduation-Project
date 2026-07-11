namespace Graduation_Project.Interfaces
{
    public interface IAdminNotificationService
    {
        Task NotifyAsync(string title, string message, string type, string? actionUrl = null, string severity = "info", bool sendPush = true);
    }
}
