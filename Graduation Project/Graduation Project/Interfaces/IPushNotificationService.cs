namespace Graduation_Project.Interfaces
{
    public interface IPushNotificationService
    {
        Task SendToUserAsync(string userId, string title, string body, string? url = null);
        Task SendToUsersAsync(IEnumerable<string> userIds, string title, string body, string? url = null);
        string GetVapidPublicKey();
    }
}
